﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Functions.Cli.Helpers;
using Azure.Functions.Cli.Tests.E2E.AzureResourceManagers;
using Azure.Functions.Cli.Tests.E2E.AzureResourceManagers.Commons;
using Azure.Functions.Cli.Tests.E2E.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Azure.Functions.Cli.Tests.E2E
{
    
    public class DeploymentTests : BaseE2ETest, IClassFixture<StorageAccountManager>, IClassFixture<ServerFarmManager>, IClassFixture<FunctionAppManager>
    {
        private readonly StorageAccountManager _storageAccountManager;
        private readonly ServerFarmManager _serverFarmManager;
        private readonly FunctionAppManager _functionAppManager;

        private readonly static string id = DateTime.Now.ToString("HHmmss");
        private readonly static string linuxStorageAccountName = $"lcte2estorage{id}";
        private readonly static string linuxConsumptionServerFarm = $"lcte2ecserverfarm{id}";
        private readonly static string linuxConsumptionPythonFunctionApp = $"lcte2ecpython{id}";
        private bool ResroucesCreated = false;

        public DeploymentTests(ITestOutputHelper output, StorageAccountManager saManager, ServerFarmManager sfManager, FunctionAppManager faManager) : base(output)
        {
            _storageAccountManager = saManager;
            _serverFarmManager = sfManager;
            _functionAppManager = faManager;

            if (EnvironmentHelper.GetEnvironmentVariableAsBool(E2ETestConstants.IsPublicBuild) == true)
            {
                // no need to create the resources for public build.
                return;
            }

            if (!EnvironmentHelper.GetEnvironmentVariableAsBool(E2ETestConstants.EnableDeploymentTests))
            {
                // no need to create the resources if deployment tests are not enabled.
                return;
            }

            try
            {
                if ((!_storageAccountManager.Contains(linuxStorageAccountName) ||
                !_serverFarmManager.Contains(linuxConsumptionServerFarm) ||
                !_functionAppManager.Contains(linuxConsumptionPythonFunctionApp)) &&
                !EnvironmentHelper.GetEnvironmentVariableAsBool(E2ETestConstants.CodeQLBuild))
                {
                    InitializeLinuxResources().Wait();
                }
                ResroucesCreated = true;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Failed to initialize resources: {ex}");
            }
            
        }

        private async Task InitializeLinuxResources()
        {
            // Create storage account and server farm
            await Task.WhenAll(
                _storageAccountManager.Create(linuxStorageAccountName, os: FunctionAppOs.Linux),
                _serverFarmManager.Create(linuxConsumptionServerFarm, os: FunctionAppOs.Linux)
            );

            // Check if creation has finished
            await Task.WhenAll(
                _storageAccountManager.WaitUntilCreated(linuxStorageAccountName),
                _serverFarmManager.WaitUntilCreated(linuxConsumptionServerFarm));

            // Acquire storage account key
            ListKeysResponse listkeys = await _storageAccountManager.WaitUntilListKeys(linuxStorageAccountName);
            string storageAccountKey = listkeys.keys.FirstOrDefault().value;

            // Create function app
            await _functionAppManager.Create(linuxConsumptionPythonFunctionApp, linuxStorageAccountName, storageAccountKey, linuxConsumptionServerFarm,
                os: FunctionAppOs.Linux, runtime: FunctionAppRuntime.Python);
            await _functionAppManager.WaitUntilSiteAvailable(linuxConsumptionPythonFunctionApp);
            await _functionAppManager.WaitUntilScmSiteAvailable(linuxConsumptionPythonFunctionApp);
        }

        [SkippableFact]
        public async void RemoteBuildPythonFunctionApp()
        {
            Skip.IfNot(ResroucesCreated, "Resources are not created");
            TestConditions.SkipIfPublicBuild();
            TestConditions.SkipIfCodeQLBuildOrEnableDeploymentTestsNotDefined();
            await CliTester.Run(new[] {
                new RunConfiguration
                {
                    Commands = new[]
                    {
                        "init . --worker-runtime python",
                        "new -l python -t HttpTrigger -n httptrigger",
                        $"azure functionapp publish {linuxConsumptionPythonFunctionApp} --build remote"
                    },
                    OutputContains = new string[]
                    {
                        "Remote build succeeded!"
                    },
                    CommandTimeout = TimeSpan.FromMinutes(5)
                },
            }, _output);
        }
    }
}
