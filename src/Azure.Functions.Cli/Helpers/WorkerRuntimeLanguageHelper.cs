﻿using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Functions.Cli.Common;
using Azure.Functions.Cli.Interfaces;
using Colors.Net;
using static Azure.Functions.Cli.Common.OutputTheme;

namespace Azure.Functions.Cli.Helpers
{
    public enum WorkerRuntime
    {
        None,
        dotnet,
        dotnetIsolated,
        node,
        python,
        java,
        powershell,
        custom
    }

    public static class WorkerRuntimeLanguageHelper
    {
        private static readonly IDictionary<WorkerRuntime, IEnumerable<string>> availableWorkersRuntime = new Dictionary<WorkerRuntime, IEnumerable<string>>
        {
            { WorkerRuntime.dotnetIsolated, new [] { "dotnet-isolated", "c#-isolated", "csharp-isolated", "f#-isolated", "fsharp-isolated" } },
            { WorkerRuntime.dotnet, new [] { "c#", "csharp", "f#", "fsharp" } },
            { WorkerRuntime.node, new [] { "js", "javascript", "typescript", "ts" } },
            { WorkerRuntime.python, new []  { "py" } },
            { WorkerRuntime.java, new string[] { } },
            { WorkerRuntime.powershell, new [] { "pwsh" } },
            { WorkerRuntime.custom, new string[] { } }
        };

        private static readonly IDictionary<string, WorkerRuntime> normalizeMap = availableWorkersRuntime
            .SelectMany(p => p.Value.Select(v => new { key = v, value = p.Key }).Append(new { key = p.Key.ToString(), value = p.Key }))
            .ToDictionary(k => k.key, v => v.value, StringComparer.OrdinalIgnoreCase);

        private static readonly IDictionary<WorkerRuntime, string> workerToDefaultLanguageMap = new Dictionary<WorkerRuntime, string>
        {
            { WorkerRuntime.dotnet, Constants.Languages.CSharp },
            { WorkerRuntime.dotnetIsolated, Constants.Languages.CSharpIsolated },
            { WorkerRuntime.node, Constants.Languages.JavaScript },
            { WorkerRuntime.python, Constants.Languages.Python },
            { WorkerRuntime.powershell, Constants.Languages.Powershell },
            { WorkerRuntime.custom, Constants.Languages.Custom },
        };

        private static readonly IDictionary<string, IEnumerable<string>> languageToAlias = new Dictionary<string, IEnumerable<string>>
        {
            // By default node should map to javascript
            { Constants.Languages.JavaScript, new [] { "js", "node" } },
            { Constants.Languages.TypeScript, new [] { "ts" } },
            { Constants.Languages.Python, new [] { "py" } },
            { Constants.Languages.Powershell, new [] { "pwsh" } },
            { Constants.Languages.CSharp, new [] { "csharp", "dotnet" } },
            { Constants.Languages.CSharpIsolated, new [] { "dotnet-isolated", "dotnetIsolated" } },
            { Constants.Languages.Java, new string[] { } },
            { Constants.Languages.Custom, new string[] { } }
        };

        public static readonly IDictionary<string, string> WorkerRuntimeStringToLanguage = languageToAlias
            .Select(p => p.Value.Select(v => new { key = v, value = p.Key }).Append(new { key = p.Key.ToString(), value = p.Key }))
            .SelectMany(i => i)
            .ToDictionary(k => k.key, v => v.value, StringComparer.OrdinalIgnoreCase);

        public static readonly IDictionary<WorkerRuntime, IEnumerable<string>> WorkerToSupportedLanguages = new Dictionary<WorkerRuntime, IEnumerable<string>>
        {
            { WorkerRuntime.node, new [] { Constants.Languages.JavaScript, Constants.Languages.TypeScript } },
            { WorkerRuntime.dotnet, new [] { Constants.Languages.CSharp, Constants.Languages.FSharp } },
            { WorkerRuntime.dotnetIsolated, new [] { Constants.Languages.CSharpIsolated, Constants.Languages.FSharpIsolated } }
        };

        public static string AvailableWorkersRuntimeString =>
            string.Join(", ", availableWorkersRuntime.Keys
                .Where(k => (k != WorkerRuntime.java))
                .Select(s => s.ToString()))
            .Replace(WorkerRuntime.dotnetIsolated.ToString(), "dotnet-isolated");

        public static string GetRuntimeMoniker(WorkerRuntime workerRuntime) 
        {
            switch(workerRuntime)
            {
                case WorkerRuntime.None:
                    return "None";
                case WorkerRuntime.dotnet:
                    return "dotnet";
                case WorkerRuntime.dotnetIsolated:
                    return "dotnet-isolated";
                case WorkerRuntime.node:
                    return "node";
                case WorkerRuntime.python:
                    return "python";
                case WorkerRuntime.java:
                    return "java";
                case WorkerRuntime.powershell:
                    return "powershell";
                case WorkerRuntime.custom:
                    return "custom";
                default:
                    return "None";
            }
        }

        public static IDictionary<WorkerRuntime, string> GetWorkerToDisplayStrings()
        {
            IDictionary<WorkerRuntime, string> workerToDisplayStrings = new Dictionary<WorkerRuntime, string>();
            foreach (WorkerRuntime wr in AvailableWorkersList)
            {
                switch (wr)
                {
                    case WorkerRuntime.dotnet:
                        workerToDisplayStrings[wr] = "dotnet (in-process model)";
                        break;
                    case WorkerRuntime.dotnetIsolated:
                        workerToDisplayStrings[wr] = "dotnet (isolated worker model)";
                        break;
                    default:
                        workerToDisplayStrings[wr] = wr.ToString();
                        break;
                }
            }
            return workerToDisplayStrings;
        }

        public static IEnumerable<WorkerRuntime> AvailableWorkersList => availableWorkersRuntime.Keys
            .Where(k => k != WorkerRuntime.java);

        public static WorkerRuntime NormalizeWorkerRuntime(string workerRuntime)
        {
            if (string.IsNullOrWhiteSpace(workerRuntime))
            {
                throw new ArgumentNullException(nameof(workerRuntime), "Worker runtime cannot be null or empty.");
            }
            else if (normalizeMap.ContainsKey(workerRuntime))
            {
                return normalizeMap[workerRuntime];
            }
            else
            {
                throw new ArgumentException($"Worker runtime '{workerRuntime}' is not a valid option. Options are {AvailableWorkersRuntimeString}");
            }
        }

        public static string NormalizeLanguage(string languageString)
        {
            if (string.IsNullOrWhiteSpace(languageString))
            {
                throw new ArgumentNullException(nameof(languageString), "language can't be empty");
            }
            else if (normalizeMap.ContainsKey(languageString))
            {
                return WorkerRuntimeStringToLanguage[languageString];
            }
            else
            {
                throw new ArgumentException($"Language '{languageString}' is not available. Available language strings are {WorkerRuntimeStringToLanguage.Keys}");
            }
        }

        public static IEnumerable<string> LanguagesForWorker(WorkerRuntime worker)
        {
            return normalizeMap.Where(p => p.Value == worker).Select(p => p.Key);
        }

        public static WorkerRuntime GetCurrentWorkerRuntimeLanguage(ISecretsManager secretsManager)
        {
            var setting = Environment.GetEnvironmentVariable(Constants.FunctionsWorkerRuntime)
                          ?? secretsManager.GetSecrets().FirstOrDefault(s => s.Key.Equals(Constants.FunctionsWorkerRuntime, StringComparison.OrdinalIgnoreCase)).Value;

            try
            {
                return NormalizeWorkerRuntime(setting);
            }
            catch
            {
                return WorkerRuntime.None;
            }
        }

        internal static WorkerRuntime SetWorkerRuntime(ISecretsManager secretsManager, string language)
        {
            var workerRuntime = NormalizeWorkerRuntime(language);
            var runtimeMoniker = GetRuntimeMoniker(workerRuntime);

            secretsManager.SetSecret(Constants.FunctionsWorkerRuntime, runtimeMoniker);

            ColoredConsole
                .WriteLine(WarningColor("Starting from 2.0.1-beta.26 it's required to set a language for your project in your settings."))
                .WriteLine(WarningColor($"Worker runtime '{runtimeMoniker}' has been set in '{SecretsManager.AppSettingsFilePath}'."));

            return workerRuntime;
        }

        public static string GetDefaultTemplateLanguageFromWorker(WorkerRuntime worker)
        {
            if (!workerToDefaultLanguageMap.ContainsKey(worker))
            {
                throw new ArgumentException($"Worker runtime '{worker}' is not a valid worker for a template.");
            }
            return workerToDefaultLanguageMap[worker];
        }

        public static bool IsDotnet(WorkerRuntime worker)
        {
            return worker == WorkerRuntime.dotnet || worker ==  WorkerRuntime.dotnetIsolated;
        }

        public static bool IsDotnetIsolated(WorkerRuntime worker)
        {
            return worker ==  WorkerRuntime.dotnetIsolated;
        }
    }
}
