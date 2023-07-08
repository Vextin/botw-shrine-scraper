using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using HtmlAgilityPack;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class Program
{
    static void Main(string[] args)
    {
        Task<string> task = GetPage("https://zelda.fandom.com/wiki/Ancient_Shrine");
        string html = task.Result;
        List<Shrine> list = GetShrineList(html);
        WriteJSON(list);
    }

    private static void WriteJSON(List<Shrine> list)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string fileName = "BOTWShrines.json";
        string jsonString = JsonSerializer.Serialize(list, options);

        Console.WriteLine(jsonString);

        File.WriteAllText(fileName, jsonString);


    }

    private static async Task<string> GetPage(string url)
    {
        HttpClient client = new HttpClient();
        var response = await client.GetStringAsync(url);
        return response;
    }

    private static List<Shrine> GetShrineList (string html)
    {
        Console.WriteLine("Starting to get shrine list.");
        List<Shrine> shrineList = new List<Shrine>();

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);
        var shrineTables = doc.DocumentNode.SelectNodes("//*[@class=\"wikitable\"]");

        Console.WriteLine("Found " + shrineTables.Count + " tables to parse.");

        //Parse each table
        foreach(var shrineTable in shrineTables)
        {
            Console.WriteLine("Parsing " + shrineTable.Name);
            //Cache location of all following shrines
            string location = shrineTable.PreviousSibling.PreviousSibling.ChildNodes[0].InnerText;
            //If we're at the translations, we're done!
            if (location.Contains("See here"))
            {
                break;
            }
            Console.WriteLine(location);

            //Get each row
            var shrineLinks = shrineTable.Descendants("tr").Where(node => node.PreviousSibling != null).ToList();

            //Parse each row 
            foreach(var shrineLink in shrineLinks) 
            {
                Shrine shrine = new Shrine();
                shrine.location = location;
                
                shrine.name = shrineLink.ChildNodes[1].ChildNodes[0].InnerText;
                shrine.id = shrineLink.ChildNodes[1].ChildNodes[0].Id;
                shrine.title = shrineLink.ChildNodes[3].ChildNodes[0].InnerText;
                shrine.granularLocation = shrineLink.ChildNodes[5].ChildNodes[0].InnerText;

                //List rewards
                
                if(shrineLink.ChildNodes.Count >= 7)
                {
                    Dictionary<string, int> rewardDict = new Dictionary<string, int>();
                    var rewards = shrineLink.ChildNodes[7].Descendants("a").ToList();
                    foreach (var reward in rewards)
                    {
                        string itemName = reward.InnerText;
                        int count = 1;
                        int xIndex = itemName.IndexOf(" x");
                        if (xIndex > 0)
                        {
                            itemName.Remove(xIndex);
                            count = int.Parse(itemName.Substring(xIndex + 2));
                        }
                        rewardDict.Add(itemName, count);
                    }
                    shrine.rewards = rewardDict;
                }
                

                string quest = GetQuest(shrine.id);

                shrine.quest = quest;
                if(quest != "")
                {
                    shrine.questRequired = false;
                }

                
                shrineList.Add(shrine);
            }
        }
        return shrineList;
    }

    private static string GetQuest(string id)
    {
        return "";
    }

}

public class Shrine
{
    [JsonInclude]
    public string id;
    [JsonInclude]
    public string name;
    [JsonInclude]
    public string title;
    [JsonInclude]
    public string location;
    [JsonInclude]
    public string granularLocation;
    [JsonInclude]
    public bool questRequired;
    [JsonInclude]
    public string quest;
    [JsonInclude]
    public Dictionary<string, int> rewards;
}