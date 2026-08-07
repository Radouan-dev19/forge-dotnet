string choice = args.Length == 1 ? args[0] : "simulate";
if (!StringComparer.Ordinal.Equals(choice, "simulate"))
{
    Console.Error.WriteLine("Ce starter exécute uniquement le mode local simulé.");
    return 2;
}

string[] checks =
[
    "hosting-choice-explicit",
    "managed-identity-required",
    "shared-keys-disabled",
    "cost-warning-present",
    "deletion-scope-dedicated",
    "telemetry-without-personal-data",
];
foreach (string check in checks)
{
    Console.WriteLine($"PASS {check}");
}
return 0;
