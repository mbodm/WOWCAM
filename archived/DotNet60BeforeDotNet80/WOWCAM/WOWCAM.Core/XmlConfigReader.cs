using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace WOWCAM.Core
{
    public sealed class XmlConfigReader : IConfigReader
    {
        private const string OperatingModeElementName = "mode";
        private const string DownloadFolderElementName = "download";
        private const string UnzipFolderElementName = "unzip";
        private const string TempFolderElementName = "temp";

        private readonly string xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");

        private readonly ICurseHelper curseHelper;

        public XmlConfigReader(ICurseHelper curseHelper)
        {
            this.curseHelper = curseHelper ?? throw new ArgumentNullException(nameof(curseHelper));
        }

        public string Storage => xmlFile;

        public OperatingMode OperatingMode { get; private set; } = OperatingMode.DownloadAndUnzip;
        public string LoadedProfile { get; private set; } = string.Empty;
        public string TempFolder { get; private set; } = string.Empty;
        public string DownloadFolder { get; private set; } = string.Empty;
        public string UnzipFolder { get; private set; } = string.Empty;
        public IEnumerable<string> AddonUrls { get; private set; } = Enumerable.Empty<string>();

        public void ReadConfig()
        {
            if (!File.Exists(xmlFile))
            {
                throw new InvalidOperationException("Config file not exists.");
            }

            /* Todo: Design of XML and XSD validation. */

            var document = XDocument.Load(xmlFile);

            if (!ValidateFileStructure())
            {
                throw new InvalidOperationException("Todo");
            }








            // General file structure is always required, but some fileds may be empty, dependent on mode.

            //var mode = document?.Element("root")?.Element(OperatingModeElementName)?.Value?.ToString() ?? string.Empty;
            //OperatingMode = Enum.TryParse(mode, out OperatingMode value) ? value : OperatingMode.DownloadAndUnzip;
            //DownloadFolder = document?.Element("root")?.Element(DownloadFolderElementName)?.Value?.ToString() ?? string.Empty;
            //UnzipFolder = document?.Element("root")?.Element(UnzipFolderElementName)?.Value?.ToString() ?? string.Empty;
            //TempFolder = document?.Element("root")?.Element(TempFolderElementName)?.Value?.ToString() ?? string.Empty;

            //AddonUrls = document?.Element("root")?.Element("addons")?.Elements()?.
            // Where(e => e.Name == "url")?.
            // Select(e => e.Value.Trim().ToLower())?.
            // Distinct() ?? Enumerable.Empty<string>();
        }

        public void ValidateConfig(bool downloadRelated, bool unzipRelated)
        {
            if (downloadRelated)
            {
                ValidateDownloadRelatedEntries();
            }

            if (unzipRelated)
            {
                ValidateUnzipRelatedEntries();
            }

            if (TempFolder != string.Empty)
            {
                ValidateFolderPath(TempFolder, TempFolderElementName);
            }
        }

        private bool ValidateFileStructure()
        {
            var xsd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
                        <xs:schema xmlns:xs=""http://www.w3.org/2001/XMLSchema"">
	                        <xs:element name=""root"">
		                        <xs:complexType>
			                        <xs:sequence>
				                        <xs:element name=""mode"" type=""xs:string"" />
				                        <xs:element name=""load"" type=""xs:string"" />
				                        <xs:element name=""temp"" type=""xs:string"" />
				                        <xs:element name=""profiles"">
					                        <xs:complexType>
						                        <xs:sequence>
							                        <xs:element name=""retail"">
								                        <xs:complexType>
									                        <xs:sequence>
										                        <xs:element name=""folders"">
											                        <xs:complexType>
												                        <xs:sequence>
													                        <xs:element name=""download"" type=""xs:string"" />
													                        <xs:element name=""unzip"" type=""xs:string"" />
												                        </xs:sequence>
											                        </xs:complexType>
										                        </xs:element>
										                        <xs:element name=""addons"">
											                        <xs:complexType>
												                        <xs:sequence>
													                        <xs:element name=""url"" maxOccurs=""unbounded"" type=""xs:string"" />
												                        </xs:sequence>
											                        </xs:complexType>
										                        </xs:element>
									                        </xs:sequence>
								                        </xs:complexType>
							                        </xs:element>
						                        </xs:sequence>
					                        </xs:complexType>
				                        </xs:element>
			                        </xs:sequence>
		                        </xs:complexType>
	                        </xs:element>
                        </xs:schema>";

            var schemas = new XmlSchemaSet();
            schemas.Add("", XmlReader.Create(new StringReader(xsd)));
            var document = XDocument.Load(xmlFile);
            var isValid = true;
            document.Validate(schemas, (o, e) =>
            {
                Debug.WriteLine(e.Message);
                isValid = false;
            });
            return isValid;
        }

        private void ValidateDownloadRelatedEntries()
        {
            if (DownloadFolder == string.Empty)
            {
                throw new InvalidOperationException("Config file contains no download folder, to download the zip files into.");
            }

            ValidateFolderPath(DownloadFolder, DownloadFolderElementName);

            if (!AddonUrls.Any())
            {
                throw new InvalidOperationException("Config file contains no urls, so there is nothing to download.");
            }

            if (AddonUrls.Any(url => !curseHelper.IsAddonPageUrl(url)))
            {
                throw new InvalidOperationException("Config file contains invalid urls, whose content is not a valid Curse addon url.");
            }
        }

        private void ValidateUnzipRelatedEntries()
        {
            if (DownloadFolder == string.Empty)
            {
                throw new InvalidOperationException("Config file contains no download folder, to unzip files from.");
            }

            ValidateFolderPath(DownloadFolder, DownloadFolderElementName);

            if (UnzipFolder == string.Empty)
            {
                throw new InvalidOperationException("Config file contains no unzip folder, to extract the zip files into.");
            }

            ValidateFolderPath(UnzipFolder, UnzipFolderElementName);
        }

        private static void ValidateFolderPath(string folderPath, string elementName)
        {
            try
            {
                Path.GetFullPath(folderPath);
            }
            catch
            {
                throw new InvalidOperationException($"Config file contains invalid <{elementName}> folder entry, whose content is not a valid filesystem path.");
            }
        }
    }
}
