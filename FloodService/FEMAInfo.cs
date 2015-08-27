using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Script.Serialization;

namespace FloodService
{

    //TESTING URL
    //http://localhost/FEMAService/FloodService.FEMAInfo.svc/GetFEMAInfo?Address=904%20Springview%20Trl,%20Garner,%20North%20Carolina,%20USA
    public class FEMAInfo : IFEMAInfo
    {


        //ESRI's Geocoding service, returns XY coordinates for addresses
        private string strGeocoderURL = "http://geocode.arcgis.com/arcgis/rest/services/World/GeocodeServer/find";

        //FEMA REST endpoint URLs
        private string strPanelSuffixURL = "http://hazards.fema.gov/gis/nfhl/rest/services/public/NFHL/MapServer/3/query";
        private string strHazardZoneURL = "http://hazards.fema.gov/gis/nfhl/rest/services/public/NFHL/MapServer/28/query";
        private string strCIDURL = "http://hazards.fema.gov/gis/nfhl/rest/services/public/NFHL/MapServer/22/query";


        [WebInvoke(Method = "GET",
                      ResponseFormat = WebMessageFormat.Json,
                      UriTemplate = "GetFEMAInfo?Address={address}")]


        public Stream GetFEMAInfo(string strAddress)
        {


             FEMAData femadata = new FEMAData();

            //Get the XY coordinates from the user-entered address
             Dictionary<string,double> dicCoords = Geocode(strAddress);

             double x, y = 0;
             x = dicCoords["x"];
             y = dicCoords["y"];

             femadata.x = x;
             femadata.y = y;


            //Get Panel and Suffix attributes using the Address XY. (Point in Polygon query)
             Dictionary<string,string> dicPanelSuffix = GetPanelandSuffix(x,y);


             femadata.Panel = dicPanelSuffix["FIRM_PAN"];
             femadata.Suffix = dicPanelSuffix["SUFFIX"];

             //Get Hazard Zone attribute using the Address XY. (Point in Polygon query)
             string strHazardZone = Get_FEMA_attribute(strHazardZoneURL, x, y, "FLD_ZONE");
             femadata.HazardZone = strHazardZone;

             //Get Community ID# attribute using the Address XY. (Point in Polygon query)
             string strCID = Get_FEMA_attribute(strCIDURL, x, y, "CID");
             femadata.CID = strCID;


             //Serialize to JSON and return
             string jsonAttributes = new JavaScriptSerializer().Serialize(femadata);
             byte[] resultBytes = Encoding.UTF8.GetBytes(jsonAttributes);

             WebOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
             return new MemoryStream(resultBytes);
            
        }




        private Dictionary<string, string> GetPanelandSuffix(double x, double y)
        {


            Dictionary<string, string> dicPanelSuffix = new Dictionary<string, string>();


            string requestUri = strPanelSuffixURL;

            //Parameters to pass in to the REST call
            StringBuilder data = new StringBuilder();
            //return results as JSON
            data.AppendFormat("?f={0}", "json");
            //Input geometety (Address XY)
            data.AppendFormat("&geometry={0},{1}",x.ToString(),y.ToString());
            //Its a point
            data.AppendFormat("&geometryType={0}", "esriGeometryPoint");
            //Return in a web mercator projection, probbaly not needed
            data.AppendFormat("&inSR={0}", "102100");
            //Do point-n-poly intersection
            data.AppendFormat("&spatialRel={0}", "esriSpatialRelIntersects");
            //The attributes to return
            data.AppendFormat("&outFields={0},{1}", "FIRM_PAN","SUFFIX");
            //Dont return the feature's geometry.  We dont need it and will slow the query
            data.AppendFormat("&returnGeometry={0}", "false");
    
              
            //Make the call and parse the results
            HttpWebRequest request = WebRequest.Create(requestUri + data) as HttpWebRequest;

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());

                string responseString = reader.ReadToEnd();

                
                System.Web.Script.Serialization.JavaScriptSerializer jss =
                    new System.Web.Script.Serialization.JavaScriptSerializer();

                IDictionary<string, object> results =
                    jss.DeserializeObject(responseString) as IDictionary<string, object>;

                if (results != null && results.ContainsKey("features"))
                {
                    IEnumerable<object> features = results["features"] as IEnumerable<object>;
                     foreach (IDictionary<string, object> feature in features)
                     {
                         IDictionary<string, object> attribute = feature["attributes"] as IDictionary<string, object>;
                         dicPanelSuffix.Add("FIRM_PAN", attribute["FIRM_PAN"].ToString());
                         dicPanelSuffix.Add("SUFFIX", attribute["SUFFIX"].ToString());
                         return dicPanelSuffix;
                     }
                }
                return null;

            }

            return dicPanelSuffix;
        }

        private string Get_FEMA_attribute(string url, double x, double y, string strAttribute)
        {
           


            string requestUri = url;

            StringBuilder data = new StringBuilder();
            data.AppendFormat("?f={0}", "json");
            data.AppendFormat("&geometry={0},{1}", x.ToString(), y.ToString());
            data.AppendFormat("&geometryType={0}", "esriGeometryPoint");
            data.AppendFormat("&inSR={0}", "102100");
            data.AppendFormat("&spatialRel={0}", "esriSpatialRelIntersects");
            data.AppendFormat("&outFields={0}", strAttribute);
            data.AppendFormat("&returnGeometry={0}", "false");


            //Make the call and parse the results
            HttpWebRequest request = WebRequest.Create(requestUri + data) as HttpWebRequest;

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());

                string responseString = reader.ReadToEnd();

              
                System.Web.Script.Serialization.JavaScriptSerializer jss =
                    new System.Web.Script.Serialization.JavaScriptSerializer();

                IDictionary<string, object> results =
                    jss.DeserializeObject(responseString) as IDictionary<string, object>;

                if (results != null && results.ContainsKey("features"))
                {
                    IEnumerable<object> features = results["features"] as IEnumerable<object>;
                    foreach (IDictionary<string, object> feature in features)
                    {
                        IDictionary<string, object> attribute = feature["attributes"] as IDictionary<string, object>;
                        return attribute[strAttribute].ToString();
                    }
                }
                return "";

            }

            return "" ;
        }



        private Dictionary<string,double> Geocode(string strAddress)
        {
            string requestUri = strGeocoderURL;

            StringBuilder data = new StringBuilder();
            //return results as JSON
            data.AppendFormat("?f={0}", "json");
            //USA search only
            data.AppendFormat("&sourceCountry={0}", "USA");
            //Return coordinates as web mercator, need this projection for FEMA query
            data.AppendFormat("&outSR={0}", "102100");
            //text is the address text
            data.AppendFormat("&text={0}", System.Web.HttpUtility.UrlEncode(strAddress));
              
            //Make the call
            HttpWebRequest request = WebRequest.Create(requestUri + data) as HttpWebRequest;

            using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
            {
                StreamReader reader = new StreamReader(response.GetResponseStream());

                string responseString = reader.ReadToEnd();

                // JavaScriptSerializer in System.Web.Extensions.dll
                System.Web.Script.Serialization.JavaScriptSerializer jss =
                    new System.Web.Script.Serialization.JavaScriptSerializer();

                IDictionary<string, object> results =
                    jss.DeserializeObject(responseString) as IDictionary<string, object>;

                if (results != null && results.ContainsKey("locations"))
                {
                    IEnumerable<object> candidates = results["locations"] as IEnumerable<object>;
                    foreach (IDictionary<string, object> candidate in candidates)
                    {
                     
                        IDictionary<string, object> location = candidate["feature"] as IDictionary<string, object>;
                        IDictionary<string, object> geom = location["geometry"] as IDictionary<string, object>;

                        Dictionary<string, double> dicCoords = new Dictionary<string, double>();

                        double x = decimal.ToDouble((decimal)geom["x"]);
                        double y = decimal.ToDouble((decimal)geom["y"]);

                        dicCoords.Add("x", x);
                        dicCoords.Add("y", y);

                        return dicCoords;
                    }
                }
                return null;
            }

        }
    }



    
    public class FEMAData
    {

        public double x { get; set; }
        public double y { get; set; }
        public string Panel { get; set; }
        public string Suffix { get; set; }
        public string HazardZone { get; set; }
        public string CID { get; set; }


    }
}
