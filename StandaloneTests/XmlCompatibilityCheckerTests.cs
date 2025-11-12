using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DS4Windows;

[TestClass]
public class XmlCompatibilityCheckerTests
{
    private string[] rootsToScan = new[] {
        "C:\\Program Files\\DS4Windows",
        "G:\\DS4Windows"
    };

    [TestMethod]
    public void RunCompatibilityScan_WriteReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("source,relativePath,wellFormed,rootName,dtoProfileDeserialize,dtoDefaultDeserialize,backingStoreLoad,notes");

        foreach (var root in rootsToScan)
        {
            if (!Directory.Exists(root))
            {
                Console.WriteLine($"Skipping missing root: {root}");
                continue;
            }

            var files = Directory.EnumerateFiles(root, "*.xml", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                bool wellFormed = false;
                string rootName = "";
                bool dtoProfile = false;
                bool dtoDefault = false;
                string backingLoad = "NA";
                string notes = string.Empty;

                try
                {
                    var doc = new XmlDocument();
                    doc.Load(file);
                    wellFormed = true;
                    rootName = doc.DocumentElement?.Name ?? "";

                    // If root is Profile or DS4Windows try DTO deserialization
                    if (string.Equals(rootName, "Profile", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var ser = new XmlSerializer(typeof(AppSettingsDTO), new XmlRootAttribute("Profile"));
                            using (var sr = new StringReader(File.ReadAllText(file)))
                            {
                                var obj = ser.Deserialize(sr) as AppSettingsDTO;
                                dtoProfile = obj != null;
                                if (dtoProfile) notes += "DTO(Profile) ok;";
                            }
                        }
                        catch (Exception ex)
                        {
                            notes += "DTO(Profile) ex:" + ex.GetType().Name + ";";
                        }

                        try
                        {
                            var ser2 = new XmlSerializer(typeof(AppSettingsDTO));
                            using (var sr2 = new StringReader(File.ReadAllText(file)))
                            {
                                var obj2 = ser2.Deserialize(sr2) as AppSettingsDTO;
                                dtoDefault = obj2 != null;
                                if (dtoDefault) notes += "DTO(default) ok;";
                            }
                        }
                        catch (Exception ex)
                        {
                            notes += "DTO(default) ex:" + ex.GetType().Name + ";";
                        }
                    }
                    else
                    {
                        // Try DTO anyway: some files use DS4Windows root etc.
                        try
                        {
                            var ser = new XmlSerializer(typeof(AppSettingsDTO), new XmlRootAttribute("Profile"));
                            using (var sr = new StringReader(File.ReadAllText(file)))
                            {
                                var obj = ser.Deserialize(sr) as AppSettingsDTO;
                                dtoProfile = obj != null;
                                if (dtoProfile) notes += "DTO(Profile) ok;";
                            }
                        }
                        catch { }

                        try
                        {
                            var ser2 = new XmlSerializer(typeof(AppSettingsDTO));
                            using (var sr2 = new StringReader(File.ReadAllText(file)))
                            {
                                var obj2 = ser2.Deserialize(sr2) as AppSettingsDTO;
                                dtoDefault = obj2 != null;
                                if (dtoDefault) notes += "DTO(default) ok;";
                            }
                        }
                        catch { }
                    }

                    // If file is named Profiles.xml, try BackingStore.Load()
                    if (string.Equals(Path.GetFileName(file), "Profiles.xml", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var bs = new BackingStore();
                            bs.m_Profile = file;
                            bool loaded = bs.Load();
                            backingLoad = loaded ? "OK" : "FAILED";
                            notes += "BackingStore.Load:" + backingLoad + ";";
                        }
                        catch (Exception ex)
                        {
                            backingLoad = "EX:" + ex.GetType().Name;
                            notes += "BackingStoreEx:" + ex.Message + ";";
                        }
                    }
                }
                catch (Exception ex)
                {
                    notes += "XmlLoadEx:" + ex.GetType().Name + ";";
                }

                var rel = Path.GetRelativePath(root, file).Replace(',', '_');
                sb.AppendLine($"\"{root}\",\"{rel}\",{wellFormed},\"{rootName}\",{dtoProfile},{dtoDefault},\"{backingLoad}\",\"{notes}\"");
            }
        }

        string outPath = Path.Combine(Directory.GetCurrentDirectory(), "xml_compat_report.csv");
        File.WriteAllText(outPath, sb.ToString());
        Console.WriteLine($"Wrote report to: {outPath}");

        // Also print a short summary
        Console.WriteLine("Scan complete. Sample output:\n" + sb.ToString().Split(new[]{'\n'}, StringSplitOptions.RemoveEmptyEntries).Take(10).Aggregate((a,b)=>a+"\n"+b));
    }
}
