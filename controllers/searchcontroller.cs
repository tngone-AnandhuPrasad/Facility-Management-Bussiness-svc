using System.IdentityModel.Tokens.Jwt;
using Amazon.S3.Model.Internal.MarshallTransformations;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Tls;
using TNG.Shared.Lib.Intefaces;
using TNG.Shared.Lib.Models.Auth;
using TNG.Shared.Lib.Mongo.Common;
using TNG.Shared.Lib.Mongo.Master;
using TNG.Shared.Lib.Mongo.Models;
using ILogger = TNG.Shared.Lib.Intefaces.ILogger;

[Route("api/[controller]/[action]")]
[ApiController]
public class SearchController : ControllerBase
{
    /// <summary>
    /// DB Context 
    /// </summary>
    /// <value></value>
    private IMongoLayer _db { get; set; }

    /// <summary>
    /// Authentication Service
    /// </summary>
    /// <value></value>
    private IAuthenticationService _authenticationService { get; set; }

    /// <summary>
    /// Http context
    /// </summary>
    /// <value></value>
    private IHttpContextAccessor _httpContext { get; set; }

    /// <summary>
    /// Http context
    /// </summary>
    /// <value></value>
    private IEmailer _emailer { get; set; }

    /// <summary>
    /// Logging service
    /// </summary>
    /// <value></value>
    private ILogger _logger { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>



    /// <summary>
    /// Constructor
    /// </summary>
    public SearchController(IMongoLayer db,
                            IAuthenticationService authService,
                            IHttpContextAccessor accessor, IEmailer emailer, ILogger logger)
    {
        this._db = db;
        this._authenticationService = authService;
        this._emailer = emailer;
        this._logger = logger;
    }




    /// <summary>
    /// Filtering with date
    /// </summary>
    /// <param name="daterequest"></param>
    /// <returns></returns> <summary>


    [HttpPost]
    public ActionResult<TotalCountView> SearchwithDate(DateSeacrhrequest daterequest)
    {
        var response = searchwithDate(daterequest);
        return response;
    }
    /// <summary>
    /// Filtering with Date and location
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public ActionResult<TotalCountView> SearchwithDateandLocation(DateLocationSeacrhrequest request)
    {
        var response = searchwithDateandLocation(request);
        return response;
    }

    /// <summary>
    /// Multilevel Filtering of Tasks
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>

    [HttpPost]
    public ActionResult<List<TaskView>> TaskFilter(TaskFilterRequest request)
    {
        var response = taskFilter(request);
        return response;
    }




    #region Private Region

    private TotalCountView searchwithDate(DateSeacrhrequest daterequest)
    {
        var response = new TotalCountView();
        try
        {
            if (_authenticationService.User.UserType == ACCOUNTS_USERTYPE_MASTER.MASTER || _authenticationService.User.UserType == ACCOUNTS_USERTYPE_MASTER.ADMIN)
            {
                var userfilter = Builders<MDBL_User>.Filter.Where(user => user.CreatedDate >= daterequest.FromDate && user.CreatedDate <= daterequest.FromDate);
                var user = this._db.LoadDocuments(MONGO_MODELS.USER, userfilter).ToList();
                response.TotalAdmin = user.Where(user => user.UserType == ACCOUNTS_USERTYPE_MASTER.ADMIN).Count();
                response.TotalClient = user.Where(user => user.UserType == ACCOUNTS_USERTYPE_MASTER.CLIENT).Count();
                response.TotalUser = response.TotalAdmin + response.TotalClient;
            }

            var taskfilter = Builders<MDBL_TASk>.Filter.Where(task => task.CreatedDate >= daterequest.FromDate && task.CreatedDate <= daterequest.FromDate);
            var task = this._db.LoadDocuments(MONGO_MODELS.TASK, taskfilter).ToList();

            response.PendingTask = task.Where(task => task.Status == "PENDING").Count();
            response.CompletedTask = task.Where(task => task.Status == "COMPLETED").Count();
        }
        catch (Exception ex)
        {
            this._logger.LogError("SEARCHCONTROLLER", "SearchwithDate", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
        }
        return response;


    }

    private TotalCountView searchwithDateandLocation(DateLocationSeacrhrequest request)
    {
        var response = new TotalCountView();
        try
        {
            if (_authenticationService.User.UserType == ACCOUNTS_USERTYPE_MASTER.MASTER || _authenticationService.User.UserType == ACCOUNTS_USERTYPE_MASTER.ADMIN)
            {
                var userfilter = Builders<MDBL_User>.Filter.Where(user => user.CreatedDate >= request.FromDate && user.CreatedDate <= request.ToDate && user.Location == request.Location);
                var user = this._db.LoadDocuments(MONGO_MODELS.USER, userfilter).ToList();
                response.TotalAdmin = user.Where(user => user.UserType == ACCOUNTS_USERTYPE_MASTER.ADMIN).Count();
                response.TotalClient = user.Where(user => user.UserType == ACCOUNTS_USERTYPE_MASTER.CLIENT).Count();
                response.TotalUser = response.TotalAdmin + response.TotalClient;
            }

            var taskfilter = Builders<MDBL_TASk>.Filter.Where(task => task.CreatedDate >= request.FromDate && task.CreatedDate <= request.ToDate && task.Location == request.Location);
            var task = this._db.LoadDocuments(MONGO_MODELS.TASK, taskfilter).ToList();

            response.PendingTask = task.Where(task => task.Status == "PENDING").Count();
            response.CompletedTask = task.Where(task => task.Status == "COMPLETED").Count();
            response.TotalTask = response.PendingTask + response.CompletedTask;
        }
        catch (Exception ex)
        {
            this._logger.LogError("SEARCHCONTROLLER", "S earchwithDateandLocation", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
        }

        return response;
    }


    private List<TaskView> taskFilter(TaskFilterRequest request)
    {
        var response = new List<TaskView>();
        try
        {
            var filter = Builders<MDBL_TASk>.Filter.Where(task => task.CreatedDate >= request.FromDate && task.CreatedDate <= request.ToDate);
            if (!string.IsNullOrEmpty(request.Concept))
            {
                filter = filter & Builders<MDBL_TASk>.Filter.Where(task => task.Concept == request.Concept);
            }
            if (!string.IsNullOrEmpty(request.Location))
            {
                filter = filter & Builders<MDBL_TASk>.Filter.Where(task => task.Location == request.Location);
            }
            if (!string.IsNullOrEmpty(request.PersontoContact))
            {
                filter = filter & Builders<MDBL_TASk>.Filter.Where(task => task.Persontocontact == request.PersontoContact);
            }
            if (!string.IsNullOrEmpty(request.Responsibility))
            {
                filter = filter & Builders<MDBL_TASk>.Filter.Where(task => task.Responsibility == request.Responsibility);
            }
            if (!string.IsNullOrEmpty(request.Priority))
            {
                filter = filter & Builders<MDBL_TASk>.Filter.Where(task => task.Priority == request.Priority);
            }
            if (!string.IsNullOrEmpty(request.Status))
            {
                filter = filter & Builders<MDBL_TASk>.Filter.Where(task => task.Status == request.Status);
            }
            var task = this._db.LoadDocuments(MONGO_MODELS.TASK, filter).ToList();
            if (task != null)
            {
                foreach (var n in task)
                {
                    var newtask = new TaskView();
                    newtask.TaskId = n.TaskId;
                    newtask.Concept = n.Concept;
                    newtask.Location = n.Location;
                    newtask.Persontocontact = n.Persontocontact;
                    newtask.Responsibility = n.Responsibility;
                    newtask.Priority = n.Priority;
                    newtask.Status = n.Status;
                    newtask.ConcernRaisedDate = n.ConcernRaisedDate;
                    newtask.RaisedTime = n.RaisedTime;
                    newtask.Aging = n.Aging;
                    newtask.ActionPlan = n.ActionPlan;
                    newtask.ApprovedQuotationDate = n.ApprovedQuotationDate;
                    newtask.MaintainenceWork = n.MaintainenceWork;
                    response.Add(newtask);
                }
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError("SEARCHCONTROLLER", "taskFilter", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
        }

        return response;
    }



}

#endregion