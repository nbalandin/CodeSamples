namespace Prospector.Shell.RestServices.Controllers
{
    using System;
    using System.Web.Http;
    using System.Net.Http;

    using Newtonsoft.Json;

    using Prospector.Data.Common.DomainModel;
    using Prospector.Data.Prospector.DomainModel.MEP;
    using Prospector.Data.SysSw.Access;
    using Prospector.Data.SysSw.Access.WS.DeviceManager;
    using Prospector.Data.SysSw.DomainModel.Device;
    using Prospector.Data.SysSw.DomainModel.DeviceManager;
    using Prospector.Data.SysSw.DomainModel.GatewayManager;
    using Prospector.Data.SysSw.DomainModel.MEP;
    using Prospector.Data.SysSw.Services.Interfaces;
    using Prospector.Shell.RestServices.Adapters;
    using Prospector.Shell.RestServices.Models.MEP;
    using Prospector.Shell.RestServices.Models.Meter;

    using Shared.SysSw;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Security.Claims;

    using ReportingCloud.Wrapper;

    /// <summary>
    /// Class MEPController.
    /// </summary>
    public class MEPController : ApiController
    {
        #region Fields

        private readonly IMEPService service;

        private readonly IMEPReadAccessor mepReadAccessor;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MEPController"/> class.
        /// </summary>
        /// <param name="service">The service.</param>
        public MEPController(IMEPService service, IMEPReadAccessor mepReadAccessor)
        {
            this.service = service;
            this.mepReadAccessor = mepReadAccessor;
        }

        protected override void Dispose(bool disposing)
        {
            this.service.Dispose();
            base.Dispose(disposing);
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Search MEPs
        /// </summary>
        /// <param name="model">Search criteria</param>
        /// <returns>Paginated list of MEPs &lt;DeviceData&gt;.</returns>
        [HttpPost]
        public PaginatedData<DeviceData> Search(MEPSearchModel model)
        {
            return this.service.Search(
                model.ParentDeviceId, 
                model.HierarchyFilter, 
                model.AttributeFilter, 
                new MEPSearchAdapter().Adapt(model));
        }

        /// <summary>
        /// Get MEP data for Edit
        /// </summary>
        /// <param name="deviceId">MEP identifier</param>
        /// <returns>MEB object containing data.</returns>
        [HttpGet]
        public MEPDetails Details(string deviceId)
        {
            return this.service.GetMEPDetails(deviceId);
        }

        /// <summary>
        /// Updates MEP
        /// </summary>
        /// <param name="parameters">The new data of MEP.</param>
        /// <returns>BusinessServiceResponse.</returns>
        [HttpPost]
        public BusinessServiceResponse Update(MEPUpdateModel parameters)
        {
            return this.service.Update(parameters.DeviceId, new MEPUpdateModelAdapter().Adapt(parameters));
        }

        /// <summary>
        /// Deletes MEP
        /// </summary>
        /// <param name="model">Delete parameters (Id + 3 flags)</param>
        /// <returns>MultipleBusinessServiceResponse.</returns>
        [HttpPost]
        public MultipleBusinessServiceResponse Delete(DeviceDeleteModel model)
        {
            // TODO Remove usage of Constants and Timeout
            return service.Delete(
                model.DeviceIds, 
                new DeleteParameters
                    {
                        Forcedelete = StandardAPIOptions.YES.Equals(model.ForceDelete, StringComparison.OrdinalIgnoreCase), 
                        Deletedatapoints =
                            StandardAPIOptions.YES.Equals(model.DeleteDataPoints, StringComparison.OrdinalIgnoreCase), 
                        Timeoutdatetime = DateTime.UtcNow.AddHours(1)
                    });
        }

        /// <summary>
        /// Performs connection fro parent getaway will connect
        /// </summary>
        /// <param name="model">The request parameters (list of Id + flags).</param>
        /// <returns>MultipleBusinessServiceResponse.</returns>
        [HttpPost]
        public MultipleBusinessServiceResponse BatchConnect(DeviceBatchConnectModel model)
        {
            // TODO: Get read of useless model
            ConnectParameters cp = new ConnectParameters
                                       {
                                           Communicationrequesttypeid = model.CommunicationRequestTypeId, 

                                           // TODO: Investigate this ExecutionDateTime= parameters.ExecutionDateTime,
                                           TimeoutDateTime = DateTime.Parse(model.TimeoutDateTime), 

                                           // TODO: Investigate this 
                                           Useencryption =
                                               model.UseEncryption == null
                                                   ? (bool?)null
                                                   : StandardAPIOptions.YES.Equals(
                                                       model.UseEncryption, 
                                                       StringComparison.OrdinalIgnoreCase) // TODO: Fix this 
                                       };
            return this.service.BatchConnect(model.DeviceIds, cp);
        }


        /// <summary>
        /// Performs Read Data command for MEP Devices
        /// </summary>
        /// <param name="parameters">The MEP read ata command parameters.</param>
        /// <returns>MultipleBusinessServiceResponse.</returns>
        [HttpPost]
        public MultipleBusinessServiceResponse ReadDataMEP(MEPReadDataParameters parameters)
        {
            var multipleBusinessServiceResponse = new MultipleBusinessServiceResponse { AccessorResponses = new List<AccessorResponse>() };
            var manager = new DeviceManager();

            if (parameters.RegisterReadSchedule == true)
            {
                var ReadScheduleResponse = manager.PerformCommand(new PerformDeviceCommandParameters { DeviceId = parameters.DeviceID, TimeoutDateTime = DateTime.UtcNow.AddDays(7) },
                    DeviceCommands.READ_READ_SCHEDULE);
                multipleBusinessServiceResponse.AccessorResponses.Add(ReadScheduleResponse);
            }

            if (parameters.OnDemandRegisterRead == true)
            {
                var OnDemandResponse = manager.PerformCommand(new PerformDeviceCommandParameters { DeviceId = parameters.DeviceID, TimeoutDateTime = DateTime.UtcNow.AddDays(7) },
                    DeviceCommands.READ_DATA_ON_DEMAND);
                multipleBusinessServiceResponse.AccessorResponses.Add(OnDemandResponse);
            }

            return multipleBusinessServiceResponse;
        }

        /// <summary>
        /// Gets Available Operations for MEP page
        /// </summary>
        /// <returns>available operations for the MEP.</returns>
        [HttpGet]
        public MEPAvailableOperations GetMEPAvailableOperations()
        {
            var principal = RequestContext.Principal as ClaimsPrincipal;
            var permissions = principal.Claims.Where(x => x.Type == "permissionId").Select(x => x.Value).ToList();
            return service.GetMEPAvailableOperations(permissions);
        }

        /// <summary>
        /// Start downloading the specified Report
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public HttpResponseMessage Download(string format, string reportType, string columns, string fileName)
        {
            IEnumerable<Object> reportObjects = null;

            Type type = null;
            String title = null;

            reportObjects = mepReadAccessor.GetMEPsAll().Cast<Object>();
            type = typeof(DeviceData);
            title = "MEPs";

            var helper = new DownloadReportHelper();
            HttpResponseMessage response = Request.CreateResponse();
            var result = helper.GetDownloadReportResponse(response, format, fileName, columns, reportObjects, type, title);
            return result;
        } 

        #endregion
    }
}