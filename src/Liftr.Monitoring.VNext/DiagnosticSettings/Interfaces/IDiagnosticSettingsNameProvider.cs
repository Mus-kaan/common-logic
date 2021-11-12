namespace Liftr.Monitoring.VNext.DiagnosticSettings.Interfaces
{
    public interface IDiagnosticSettingsNameProvider
    {
        string GetDiagnosticSettingNameForResourceV2();
        string GetPrefixedResourceProviderName();
        string GetPrefixedWithObservabilityResourceProviderName();
    }
}