using BackendTestingStudio.Core.Scenarios;

namespace BackendTestingStudio.Core.Reporting;

public interface IReportEngine
{
    ExecutionReport CreateReport(ScenarioExecutionResult execution);

    string Export(ExecutionReport report, ReportExportFormat format);
}
