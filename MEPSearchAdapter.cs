namespace Prospector.Shell.RestServices.Adapters
{
    using Prospector.Data.Common.DomainModel.Filter;
    using Prospector.Shell.RestServices.Common.Adapters;
    using Prospector.Shell.RestServices.Models.MEP;

    internal class MEPSearchAdapter : CombinedModelToFilterAdapter<MEPSearchModel, PagedResultFilter>
    {
        public override PagedResultFilter Adapt(MEPSearchModel source)
        {
            var gridExtraFilter = FilterAdapter.Adapt(source.ExtraFilter);
            CombinedGroup.Conditions[0].Group = gridExtraFilter == null ? null : gridExtraFilter.Group;

            AddGenericCondition(FieldType.String, "Description", source.Description);
            AddGenericCondition(FieldType.QuotedList, "Status", source.Status);
            AddGenericCondition(FieldType.String, "Name", source.Name);
            AddGenericCondition(FieldType.String, "SerialNumber", source.SerialNumber);
            AddGenericCondition(FieldType.QuotedList, "TimeZone", source.TimeZone);
            AddGenericCondition(FieldType.DateTime, "InstallationDateTime", source.InstallationDate);

            return new PagedResultFilter
                       {
                           Group = CombinedGroup, 
                           PageIndex = source.Page, 
                           PageSize = source.Rows, 
                           SortFieldName = source.OrderBy, 
                           SortType =
                               source.OrderDirection.ToLower() == "asc"
                                   ? SortType.Ascending
                                   : source.OrderDirection.ToLower() == "desc"
                                         ? SortType.Descending
                                         : (SortType?)null
                       };
        }
    }
}
