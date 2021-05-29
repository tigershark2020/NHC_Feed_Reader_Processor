using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;

namespace NHC_Feed_Reader
{
    class Program
    {
        public struct ConfigFiles
        {
            public string mysqlCredentialsSelect_JsonFile { get; set; }
            public string mysqlCredentialsInsert_JsonFile { get; set; }
            public string userSites_JsonFile { get; set; }
        }

        public struct HTMLResponse
        {
            public HttpStatusCode ResponseCode { get; set; }
            public string HTML { get; set; }
        }
        public class Event_Notification
        {
            public string eventNotification_Agency { get; set; }
            public string eventNotification_Title { get; set; }
            public string eventNotification_URL { get; set; }
            public string eventNotification_ImageURL { get; set; }
            public long eventNotification_DatetimeEpoch { get; set; }
            public string eventNotification_Category { get; set; }
            public string eventNotification_Type { get; set; }
            public string eventNotification_UniqueID { get; set; }
            public double eventNotification_Latitude { get; set; }
            public double eventNotification_Longitude { get; set; }
        }
        public class NHC_Cyclone
        {
            public string Center { get; set; }
            public string Type { get; set; }
            public string Name { get; set; }
            public string Wallet { get; set; }
            public string ATCF { get; set; }
            public string Datetime { get; set; }
            public string Movement { get; set; }
            public string Pressure { get; set; }
            public string Wind { get; set; }
            public string Headline { get; set; }
        }
        public class NHC_Item
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string pubDate { get; set; }
            public string Link { get; set; }
            public string GUID { get; set; }
            public string Author { get; set; }
            public NHC_Cyclone Cyclone_Details { get; set; }
        }

        public class Coordinates
        {
            public string Name { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        public class HurricaneSeasonStatus
        {
            bool isHurricaneSeason { get; set; }
        }
        public static void Add_Event_Notification(ConfigFiles jsonConfigPaths, Event_Notification eventNotification)
        {

            MySql.Data.MySqlClient.MySqlConnection conn;

            conn = new MySql.Data.MySqlClient.MySqlConnection();

            MySqlConnectionStringBuilder conn_string_builder = new MySqlConnectionStringBuilder();
            string json = System.IO.File.ReadAllText(jsonConfigPaths.mysqlCredentialsInsert_JsonFile);
            conn_string_builder = JsonConvert.DeserializeObject<MySqlConnectionStringBuilder>(json);

            conn = new MySqlConnection(conn_string_builder.ToString());
            try
            {
                conn.Open();
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro);
            }

            try
            {
                MySqlCommand cmd = conn.CreateCommand();
                cmd.Connection = conn;

                cmd.CommandText = "INSERT INTO `event_data`.`geo_events` (`geo_event_agency`,`geo_event_title`,`geo_event_url`,`geo_event_starttime`,`geo_event_category`,`geo_event_type`,`geo_event_ident`,`geo_event_location_latitude`,`geo_event_location_longitude`,`geo_event_notify`,`geo_event_image_url`) VALUES (@event_notification_agency,@event_notification_title,@event_notification_url,FROM_UNIXTIME(@event_notification_datetime),@event_notification_category,@event_notification_type,@event_notification_ident,@event_notification_latitude,@event_notification_longitude,1,@event_notification_image_url);";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@event_notification_agency", eventNotification.eventNotification_Agency);
                cmd.Parameters.AddWithValue("@event_notification_title", eventNotification.eventNotification_Title);
                cmd.Parameters.AddWithValue("@event_notification_url", eventNotification.eventNotification_URL);
                cmd.Parameters.AddWithValue("@event_notification_datetime", eventNotification.eventNotification_DatetimeEpoch);
                cmd.Parameters.AddWithValue("@event_notification_category", eventNotification.eventNotification_Category);
                cmd.Parameters.AddWithValue("@event_notification_type", eventNotification.eventNotification_Type);
                cmd.Parameters.AddWithValue("@event_notification_ident", eventNotification.eventNotification_UniqueID);
                cmd.Parameters.AddWithValue("@event_notification_latitude", eventNotification.eventNotification_Latitude);
                cmd.Parameters.AddWithValue("@event_notification_longitude", eventNotification.eventNotification_Longitude);
                cmd.Parameters.AddWithValue("@event_notification_image_url", eventNotification.eventNotification_ImageURL);

                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
            catch (MySqlException ex)
            {
                int errorcode = ex.Number;
                if (errorcode != 1062)
                {
                    Console.WriteLine("Notification Error:\t" + ex.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            try
            {
                MySqlCommand cmd = conn.CreateCommand();
                cmd.Connection = conn;

                cmd.CommandText = "UPDATE `event_data`.`geo_events` SET `geo_event_notified` = NOW() WHERE `geo_event_agency` = @event_notification_agency AND `geo_event_title` = @event_notification_title AND `geo_event_starttime` < DATE_SUB(NOW(), INTERVAL 120 MINUTE);";
                cmd.Prepare();

                cmd.Parameters.AddWithValue("@event_notification_agency", eventNotification.eventNotification_Agency);
                cmd.Parameters.AddWithValue("@event_notification_title", eventNotification.eventNotification_Title);

                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
            catch (MySqlException ex)
            {
                int errorcode = ex.Number;
                if (errorcode != 1062)
                {
                    Console.WriteLine("Notification Error:\t" + ex.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            conn.Close();

        }

        public static double distance(double lat1, double lon1, double lat2, double lon2, String unit)
        {
            double theta = lon1 - lon2;
            double dist = Math.Sin(deg2rad(lat1)) * Math.Sin(deg2rad(lat2)) + Math.Cos(deg2rad(lat1)) * Math.Cos(deg2rad(lat2)) * Math.Cos(deg2rad(theta));
            dist = Math.Acos(dist);
            dist = rad2deg(dist);
            dist = dist * 60 * 1.1515;
            if (unit == "K")
            {
                dist = dist * 1.609344;
            }
            else if (unit == "N")
            {
                dist = dist * 0.8684;
            }

            return (dist);
        }

        private static double deg2rad(double deg)
        {
            return (deg * Math.PI / 180.0);
        }

        private static double rad2deg(double rad)
        {
            return (rad * 180 / Math.PI);
        }

        public static List<Coordinates> Get_US_Shoreline_Coordinates_List(ConfigFiles jsonConfigPaths)
        {
            List<Coordinates> shoreLineCoordinatesList = new List<Coordinates>();

            MySql.Data.MySqlClient.MySqlConnection conn;
            conn = new MySql.Data.MySqlClient.MySqlConnection();

            MySqlConnectionStringBuilder conn_string_builder = new MySqlConnectionStringBuilder();

            string json = System.IO.File.ReadAllText(jsonConfigPaths.mysqlCredentialsSelect_JsonFile);
            conn_string_builder = JsonConvert.DeserializeObject<MySqlConnectionStringBuilder>(json);

            conn = new MySqlConnection(conn_string_builder.ToString());

            try
            {
                conn.Open();
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro);
            }

            MySqlCommand cmd = conn.CreateCommand();

            cmd.CommandText = "SELECT `NAME`,`latitude`,`longitude` FROM `geo_data`.`us_coastline`;";

            try
            {
                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Coordinates shorelineCoordinate = new Coordinates();
                    shorelineCoordinate.Name = reader[0].ToString();
                    shorelineCoordinate.Latitude = Double.Parse(reader[1].ToString());
                    shorelineCoordinate.Longitude = Double.Parse(reader[2].ToString());
                    shoreLineCoordinatesList.Add(shorelineCoordinate);
                }

                conn.Close();
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro);
            }

            return shoreLineCoordinatesList;
        }

        public static Coordinates Nearest_Shoreline_Coordinates(Coordinates hurricaneCoordinates, List<Coordinates> shoreLineCoordinatesList)
        {
            Coordinates nearestShoreLineCoordinates = new Coordinates();
            double nearestDistance = 25000;

            Console.WriteLine("Storm Coordinates:\t" + hurricaneCoordinates.Latitude + "," + hurricaneCoordinates.Longitude);

            foreach (Coordinates shorelineCoordinates in shoreLineCoordinatesList)
            {
                double distanceToShorelineCoordinate = distance(hurricaneCoordinates.Latitude, hurricaneCoordinates.Longitude, shorelineCoordinates.Latitude, shorelineCoordinates.Longitude, "N");

                if (distanceToShorelineCoordinate < nearestDistance)
                {
                    nearestDistance = distanceToShorelineCoordinate;
                    nearestShoreLineCoordinates = shorelineCoordinates;
                }

            }

            return nearestShoreLineCoordinates;
        }

        public static void Process_NHC_Feed(ConfigFiles jsonConfigPaths, string basin, List<Coordinates> shorelineCoordinatesList)
        {
            string feedURL = "https://www.nhc.noaa.gov/gis-" + basin + ".xml";

            XNamespace nhcNS = "https://www.nhc.noaa.gov";

            string Title, pubDate, Description;

            var responseXml = XDocument.Load(feedURL);
            try
            {
                Title = responseXml.Element("rss").Element("channel").Element("title").Value.ToString();
                Console.WriteLine(Title);

                pubDate = responseXml.Element("rss").Element("channel").Element("pubDate").Value.ToString();
                Console.WriteLine(pubDate);
            }
            catch (Exception e)
            {
                // Console.Read();
            }

            try
            {
                Description = responseXml.Element("rss").Element("channel").Element("description").Value.ToString();

                if (Description.Length < 1)
                {
                    Description = responseXml.Element("rss").Element("channel").Element("description").Value.ToString();
                    //  Description = responseXml.Element("rss").Element("channel").Element(ns + "summary").Value.ToString();

                    Console.WriteLine(Description);
                }

                Console.WriteLine(Description);
            }
            catch (Exception e)
            {
                // Console.Read();
            }


            foreach (var item in responseXml.Descendants("item"))
            {
                NHC_Item Cyclone_Feed_Item = new NHC_Item();
                NHC_Cyclone Cyclone_Details = new NHC_Cyclone();

                Cyclone_Feed_Item.Title = item.Element("title").Value.ToString();
                Console.WriteLine(Cyclone_Feed_Item.Title);

                Cyclone_Feed_Item.Description = item.Element("description").Value.ToString();
                Console.WriteLine(Cyclone_Feed_Item.Description);

                Cyclone_Feed_Item.pubDate = item.Element("pubDate").Value.ToString();
                Console.WriteLine(Cyclone_Feed_Item.pubDate);

                Cyclone_Feed_Item.Link = item.Element("link").Value.ToString();
                Console.WriteLine(Cyclone_Feed_Item.Link);

                Cyclone_Feed_Item.GUID = item.Element("guid").Value.ToString();
                Console.WriteLine(Cyclone_Feed_Item.GUID);

                Cyclone_Feed_Item.Author = item.Element("author").Value.ToString();
                Console.WriteLine(Cyclone_Feed_Item.Author);

                var descElements = item.Elements(nhcNS + "Cyclone");

                foreach (XElement nhcElement in item.Descendants(nhcNS + "Cyclone"))
                {
                    try
                    {
                        Cyclone_Details.Center = nhcElement.Element(nhcNS + "center").Value.ToString();
                    }
                    catch (Exception nhcElement_parse)
                    {

                    }

                    try
                    {
                        Cyclone_Details.Type = nhcElement.Element(nhcNS + "type").Value.ToString();
                    }
                    catch (Exception nhcElement_parse)
                    {

                    }

                    try
                    {
                        Cyclone_Details.Name = nhcElement.Element(nhcNS + "name").Value.ToString();
                    }
                    catch (Exception nhcElement_parse)
                    {

                    }

                    try
                    {
                        Cyclone_Details.Wallet = nhcElement.Element(nhcNS + "wallet").Value.ToString();
                    }
                    catch (Exception nhcElement_parse)
                    {

                    }

                    try
                    {
                        Cyclone_Details.ATCF = nhcElement.Element(nhcNS + "atcf").Value.ToString();
                    }
                    catch (Exception nhcElement_parse)
                    {

                    }

                    try
                    {
                        Cyclone_Details.Datetime = nhcElement.Element(nhcNS + "datetime").Value.ToString();
                    }
                    catch (Exception nhcElement_parse)
                    {

                    }

                    try
                    {
                        Cyclone_Details.Wind = nhcElement.Element(nhcNS + "wind").Value.ToString();
                    }
                    catch (Exception nhcElement_parse)
                    {

                    }

                    try
                    {
                        Cyclone_Details.Headline = nhcElement.Element(nhcNS + "headline").Value.ToString();
                    }
                    catch (Exception nhcElement_parse)
                    {

                    }

                }

                if (Cyclone_Details.Center != null)
                {
                    Cyclone_Feed_Item.Cyclone_Details = Cyclone_Details;
                    string NHC_Feed_Item_Json = JsonConvert.SerializeObject(Cyclone_Feed_Item);

                    string[] centerCoordinatesArray = Cyclone_Feed_Item.Cyclone_Details.Center.Split(",");
                    double centerLatitude = Double.Parse(centerCoordinatesArray[0].Trim());
                    double centerLongitude = Double.Parse(centerCoordinatesArray[1].Trim());

                    Coordinates currentLocation = new Coordinates();
                    currentLocation.Latitude = centerLatitude;
                    currentLocation.Longitude = centerLongitude;

                    Coordinates nearestShorelineCoordinates = Nearest_Shoreline_Coordinates(currentLocation, shorelineCoordinatesList);
                    Console.WriteLine("Nearest Shoreline Coordinate:\t" + nearestShorelineCoordinates.Latitude + "," + nearestShorelineCoordinates.Longitude);

                    DateTime dateValue = DateTime.ParseExact(Cyclone_Feed_Item.pubDate, "ddd, dd MMM yyyy HH:mm:ss GMT", CultureInfo.InvariantCulture).ToUniversalTime();

                    string unique_id_string = centerLatitude.ToString() + "," + centerLongitude.ToString();
                    Console.WriteLine(unique_id_string);
                    Event_Notification eventNotification = new Event_Notification();
                    eventNotification.eventNotification_Agency = "48945";
                    eventNotification.eventNotification_Category = "NHCAdvisory";
                    eventNotification.eventNotification_Type = "NHCAdvisory";
                    eventNotification.eventNotification_Title = Cyclone_Feed_Item.Title;
                    eventNotification.eventNotification_Latitude = centerLatitude;
                    eventNotification.eventNotification_Longitude = centerLongitude;
                    eventNotification.eventNotification_UniqueID = Cyclone_Feed_Item.Cyclone_Details.ATCF + " " + unique_id_string;
                    TimeSpan t = dateValue - new DateTime(1970, 1, 1);
                    long dateTimeEpoch = (long)t.TotalSeconds;

                    eventNotification.eventNotification_DatetimeEpoch = dateTimeEpoch;
                    if(basin.Equals("at"))
                    {
                        eventNotification.eventNotification_ImageURL = "https://www.nhc.noaa.gov/xgtwo/two_atl_5d0.png";
                    }
                    if(basin.Equals("ep"))
                    {
                        eventNotification.eventNotification_ImageURL = "https://www.nhc.noaa.gov/xgtwo/two_pac_5d0.png";
                    }
                    if(basin.Equals("cp"))
                    {
                        eventNotification.eventNotification_ImageURL = "https://www.nhc.noaa.gov/xgtwo/two_cpac_5d0.png";
                    }

                    Add_Event_Notification(jsonConfigPaths, eventNotification);
                }
            }
        }

        public static void Generate_Google_Maps_Marker_File(string event_list)
        {
            string markerString = "<markers>\n";

            MySql.Data.MySqlClient.MySqlConnection conn;

            conn = new MySql.Data.MySqlClient.MySqlConnection();

            MySqlConnectionStringBuilder conn_string_builder = new MySqlConnectionStringBuilder();
            conn_string_builder.Server = "localhost";
            conn_string_builder.UserID = "geo_events_add";
            conn_string_builder.Password = "2Hx3QqMlTs_v&i-=ecyfXgnAo+";
            conn_string_builder.Database = "event_data";

            conn = new MySqlConnection(conn_string_builder.ToString());
            try
            {
                conn.Open();
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro);
            }



            try
            {
                MySqlCommand cmd = conn.CreateCommand();

                cmd.CommandText = "SELECT `geo_id`,`geo_event_agency`,`geo_event_title`,`geo_event_description`,`geo_event_starttime`, `geo_event_location_latitude`,`geo_event_location_longitude` FROM `event_data`.`geo_events` WHERE `geo_event_type` = @event_list AND DATE_SUB(NOW(),INTERVAL 5 DAY) < `geo_event_starttime`;";
                cmd.Parameters.AddWithValue("@event_list", event_list);
                cmd.Prepare();

                MySqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    string geo_id = reader[0].ToString();
                    string geo_event_agency = reader[1].ToString();
                    string geo_event_title = reader[2].ToString();
                    string geo_event_description = reader[3].ToString();
                    string geo_event_starttime = reader[4].ToString();
                    string geo_event_location_latitude = reader[5].ToString();
                    string geo_event_location_longitude = reader[6].ToString();
                    markerString = markerString + "\t<marker id='" + geo_id + "' name='" + geo_event_title + "' address='' lat='" + geo_event_location_latitude + "' lng='" + geo_event_location_longitude + "' type='" + event_list + "'/>\n";
                }

                cmd.Dispose();
                conn.Close();
            }
            catch (Exception erro)
            {
                Console.WriteLine(erro.Message);
            }

            markerString = markerString + "</markers>";

            File.WriteAllText(@"D:\Web\live\xml\nhc_advisories.xml", markerString);

        }

        public static void Update_Hurricane_Details(bool isHurricaneSeason)
        {
            string configFilePaths = "C:/Users/tigershark2020/Documents/Credentials/Events/filePaths.json";
            bool exists = File.Exists(configFilePaths);
            string json = null;

            try
            {
                json = System.IO.File.ReadAllText(configFilePaths, System.Text.Encoding.UTF8);
            }
            catch (Exception json_read)
            {
                Console.WriteLine(json_read.Message);
            }


            string[] basinArray = { "at", "ep", "cp" };

            if (json != null) // Check That JSON String Read Above From File Contains Data
            {
                ConfigFiles jsonConfigPaths = new ConfigFiles();
                jsonConfigPaths = JsonConvert.DeserializeObject<ConfigFiles>(json);
                List<Coordinates> shorelineCoordinatesList = Get_US_Shoreline_Coordinates_List(jsonConfigPaths);

                foreach (string basin in basinArray)
                {
                    if(basin.Equals("ep"))
                    {
                        Process_NHC_Feed(jsonConfigPaths, basin, shorelineCoordinatesList);
                        string event_list = "NHCAdvisory";
                        Generate_Google_Maps_Marker_File(event_list);
                    }
                }
            }
        }

        static void Main(string[] args)
        {
            DateTime currentDate = DateTime.Now;
            DateTime hurricaneSeasonStart = new DateTime(currentDate.Year, 6, 1);
            DateTime hurricaneSeasonEnd = new DateTime(currentDate.Year, 11, 30);

            DateTime easternNorthPacificHurricaneSeasonStart = new DateTime(currentDate.Year, 5, 15);

            bool isHurricaneSeason = ((currentDate >= hurricaneSeasonStart) && (currentDate >= hurricaneSeasonEnd)) ? true : false;
            bool isHurricaneSeasonEasternNorthPacific = ((currentDate >= easternNorthPacificHurricaneSeasonStart) && (currentDate >= hurricaneSeasonEnd)) ? true : false;

            string hurricaneSeasonStartString = hurricaneSeasonStart.ToString();
            string hurricaneSeasonEndString = hurricaneSeasonEnd.ToString();

            if (isHurricaneSeasonEasternNorthPacific)
            {
                if (isHurricaneSeason)
                {
                    Update_Hurricane_Details(isHurricaneSeason);
                }
                /*
                else
                {
                    Console.WriteLine("Hurricane season runs between " + hurricaneSeasonStart.ToString() + " and " + hurricaneSeasonEnd.ToString());
                }
                */
            }
            /*
            else
            {
                Console.WriteLine("Hurricane season runs between " + hurricaneSeasonStart.ToString() + " and " + hurricaneSeasonEnd.ToString());
                Console.WriteLine("Eastern North Pacific Hurricane season runs between " + easternNorthPacificHurricaneSeasonStart.ToString() + " and " + hurricaneSeasonEnd.ToString());
            }
            */
        }
    }
}
