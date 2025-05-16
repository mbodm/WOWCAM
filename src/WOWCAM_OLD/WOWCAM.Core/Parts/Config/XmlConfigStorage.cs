using WOWCAM.Core.Parts.Logging;
using WOWCAM.Core.Parts.System;

namespace WOWCAM.Core.Parts.Config
{
    public sealed class XmlConfigStorage(ILogger logger, IReliableFileOperations reliableFileOperations) : IConfigStorage
    {
        private readonly ILogger logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IReliableFileOperations reliableFileOperations = reliableFileOperations ?? throw new ArgumentNullException(nameof(reliableFileOperations));

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
                		<theme>system</theme>
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
                await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            await File.WriteAllTextAsync(xmlFile, s, cancellationToken).ConfigureAwait(false);
            await reliableFileOperations.WaitAsync(cancellationToken).ConfigureAwait(false);

            logger.LogMethodExit();
        }
    }
}
