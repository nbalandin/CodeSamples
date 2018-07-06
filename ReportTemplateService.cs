namespace BusinessServices.Reporting.Implementation
{
    using System.Collections.Generic;

    using BusinessServices.Reporting.Interfaces;

    using DataAccess.Reporting.Interfaces;

    using DomainModel.Reporting;
    using DomainModel.Reporting.ReportTemplate;

    using Prospector.Audit;
    using Prospector.Data.Common.DomainModel;
    using Prospector.Data.Common.DomainModel.Filter;

    public class ReportTemplateService : BaseReportService, IReportTemplateService
    {
        private readonly IReportTemplateAccessor rdAccessor;

        public ReportTemplateService(IReportTemplateAccessor rdAccessor, IAuditLogAccessor alAccessor)
            : base(alAccessor)
        {
            this.rdAccessor = rdAccessor;
        }

        [AuditLogger(OperationName = "GetReportTemplatesMinimal")]
        public IEnumerable<ReportTemplateMinimal> GetReportTemplates(string reportDefinitionId)
        {
            return rdAccessor.GetReportTemplates(reportDefinitionId);
        }

        [AuditLogger(OperationName = "GetReportTemplates")]
        public PaginatedData<ReportTemplateBase> GetReportTemplates(PagedResultFilter filter)
        {
            return rdAccessor.GetReportTemplatesPage(filter);
        }

        [AuditLogger(OperationName = "GetReportTemplate")]
        public ReportTemplate GetReportTemplate(string id)
        {
            return rdAccessor.GetReportTemplate(id);
        }

        [AuditLogger(OperationName = "ReportTemplateAdd")]
        public ReportTemplateAddResponse Add(string definitionId, string name, string description, string rdl)
        {
            return rdAccessor.Add(new ReportTemplate() { Name = name, Description = description, Rdl = rdl, DefinitionId = definitionId });
        }

        [AuditLogger(OperationName = "ReportTemplateDelete")]
        public ReportDeleteResponse Delete(string[] ids)
        {
            var accessorResponse = new ReportDeleteResponse();
            foreach (var id in ids)
            {
                //TODO hadle error
                rdAccessor.Delete(id);
            }
            return accessorResponse;
        }

        [AuditLogger(OperationName = "ReportTemplateUpdate")]
        public void Update(ReportTemplate reportTemplate)
        {
            rdAccessor.Update(reportTemplate);
        }
    }
}