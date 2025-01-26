using WOWCAM.Core.Parts.Logging;

namespace WOWCAM.Core.Parts.Config
{
    public sealed class XmlConfigStorage(ILogger logger) : IConfigStorage
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private readonly string xmlFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MBODM", "WOWCAM.xml");

        public string StorageInformation => xmlFile;
        public bool StorageExists => File.Exists(xmlFile);

        public async Task CreateStorageWithDefaultsAsync(CancellationToken cancellationToken = default)
        {
            logger.LogMethodEntry();

            var s = """
                <?xml version="1.0" encoding="utf-8"?>
                <!-- ===================================================================== -->
                <!-- Please have a look at https://github.com/mbodm/wowcam for file format -->
                <!-- ===================================================================== -->
                <wowcam>
                	<general>
                		<profile>retail</profile>
                		<temp>%TEMP%</temp>
                	</general>
                	<options>
                		<autoupdate>false</autoupdate>
                		<silentmode>false</silentmode>
                	</options>
                	<profiles>
                		<retail>
                			<folder>%PROGRAMFILES(X86)%\World of Warcraft\_retail_\Interface\AddOns</folder>
                			<addons>
                				<url>https://www.curseforge.com/wow/addons/deadly-boss-mods</url>
                				<url>https://www.curseforge.com/wow/addons/details</url>
                				<url>https://www.curseforge.com/wow/addons/weakauras-2</url>
                			</addons>
                		</retail>
                	</profiles>
                </wowcam>
                """;

            s += Environment.NewLine;

            var configFolder = Path.GetDirectoryName(xmlFile) ?? throw new InvalidOperationException("Could not get directory of file path.");
            if (!Directory.Exists(configFolder))
            {
                Directory.CreateDirectory(configFolder);
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }

            await File.WriteAllTextAsync(xmlFile, s, cancellationToken).ConfigureAwait(false);
            await Task.Delay(100, cancellationToken).ConfigureAwait(false);

            logger.LogMethodExit();
        }
    }
}
