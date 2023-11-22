using CommandLine;
using NuGet.Publish;

// Parse our command line arguments and verbs then execute the appropriate commands
await Parser.Default
    .ParseArguments<NuGetPublishGenerateConfigurationInputModel, NuGetPublishGeneratePackageNameInputModel,
        NuGetPublishGenerateVersionInputModel, NuGetPublishPublishInputModel>(args).WithParsedAsync(o =>
        o.GetType() == typeof(NuGetPublishPublishInputModel)
            ?
            NuGetPublishService.NewInstance((NuGetPublishPublishInputModel)o).ExecuteAsync()
            : o.GetType() == typeof(NuGetPublishGenerateConfigurationInputModel)
                ? NuGetPublishService
                    .NewInstance(((NuGetPublishGenerateConfigurationInputModel)o).ToPublishInputModel())
                    .GenerateNuGetConfigurationAsync()
                : o.GetType() == typeof(NuGetPublishGeneratePackageNameInputModel)
                    ? NuGetPublishService
                        .NewInstance(((NuGetPublishGeneratePackageNameInputModel)o).ToPublishInputModel())
                        .GeneratePackageNameAsync()
                    : NuGetPublishService.NewInstance(((NuGetPublishGenerateVersionInputModel)o).ToPublishInputModel())
                        .GenerateVersionAsync());
