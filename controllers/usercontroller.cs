using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Tls;
using TNG.Shared.Lib.Intefaces;
using TNG.Shared.Lib.Models.Auth;
using TNG.Shared.Lib.Mongo.Common;
using TNG.Shared.Lib.Mongo.Master;
using TNG.Shared.Lib.Mongo.Models;
using ILogger = TNG.Shared.Lib.Intefaces.ILogger;

[Route("api/[controller]/[action]")]
[ApiController]
public class UserController : ControllerBase
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
    public UserController(IMongoLayer db,
                            IAuthenticationService authService,
                            IHttpContextAccessor accessor, IEmailer emailer, ILogger logger)
    {
        this._db = db;
        this._authenticationService = authService;
        this._emailer = emailer;
        this._logger = logger;
    }
    /// <summary>
    /// To insert a Task
    /// </summary>
    /// <param name="newtask"></param>
    /// <returns></returns>


    [HttpPost]
    public ActionResult<FacilityResponse> AddaTask(TaskCreation newtask)
    {

        var response = addatask(newtask);
        return response;
    }
    /// <summary>
    /// To edit the profile 
    /// </summary>
    /// <param name="edit"></param>
    /// <returns></returns>

    [HttpPost]
    public ActionResult<FacilityResponse> EditProfile(Editprofilerequest edit)
    {
        var response = editprofile(edit);
        return response;
    }

    /// <summary>
    /// To edit a user profile by Master
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>

    [HttpPost]
    [TNGAuth(ACCOUNTS_USERTYPE_MASTER.MASTER)]
    public ActionResult<FacilityResponse> Edituser(Edituserprofilerequest request)
    {
        var response = edituserprofile(request);
        return response;
    }

    /// <summary>
    /// To edit a Task
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost]
    public ActionResult<FacilityResponse> EditaTask(EditTaskRequest request)
    {
        var response = editTask(request);
        return response;
    }

    /// <summary>
    /// To get all the Tasks
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ActionResult<TaskDetails> GetALLTask()
    {
        var response = getalltask();
        return response;
    }

    /// <summary>
    /// To get all the Pending Tasks
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public ActionResult<List<TaskView>> Pendingtasks()
    {
        var response = GetPendingtasks();
        return response;
    }

    /// <summary>
    /// To get all the Compleeted Tasks
    /// </summary>
    /// <returns></returns>

    [HttpGet]
    public ActionResult<List<TaskView>> Completedtasks()
    {
        var response = GetCompletedtasks();
        return response;
    }


    /// <summary>
    /// To get all the Users
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    [HttpGet]
    public ActionResult<List<GetUserResponse>> Getusers()
    {

        var response = getAllUsers();
        return response;
    }


    /// <summary>
    /// To get Count of all
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    [HttpPost]
    public ActionResult<TotalCountView> ViewCount(string UserType)
    {
        var response = viewCount(UserType);
        return response;
    }


    #region Private Region


    private FacilityResponse addatask(TaskCreation task)
    {

        var response = new FacilityResponse();

        try
        {

            var newtask = new MDBL_TASk();
            newtask.TaskId = Guid.NewGuid().ToString();
            newtask.Concept = task.Concept;
            newtask.Location = task.Location;
            newtask.MaintainenceWork = task.MaintainenceWork;
            newtask.Persontocontact = task.PersontoContact;
            newtask.Responsibility = task.Responsibility;
            newtask.ConcernRaisedDate = task.ConcernRaisedDate;
            newtask.RaisedTime = task.RaisedTime;
            newtask.Priority = task.Priority;
            newtask.Status = task.Status;
            newtask.Aging = task.Aging;
            newtask.ApprovedQuotationDate = task.ApprovedQuotationDate;
            newtask.ActionPlan = task.ActionPlan;
            newtask.remarks = null;
            newtask.IsDeleted = false;
            newtask.CreatedDate = DateTime.Now;
            newtask.UpdatedDate = DateTime.Now;
            this._db.InsertDocument(MONGO_MODELS.TASK, newtask);
            var notification = new MDBL_Notification();
            notification.IsRead = false;
            notification.Message = "Task addedd Successfully";
            notification.CreatedDate = DateTime.UtcNow;
            this._db.InsertDocument(MONGO_MODELS.NOTIFICATIONS, notification);
            response.IsSuccess = true;
            response.Error = null;

        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.Error = ERROR_SETTINGS.TASK_FAILURE;
            this._logger.LogError("USERCONTROLLER", "AddaTask", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
        }

        return response;
    }


    private FacilityResponse editprofile(Editprofilerequest edit)
    {
        var response = new FacilityResponse();

        try
        {

            var filter = Builders<MDBL_User>.Filter.Eq(user => user.UserId, _authenticationService.User.UserId);
            var user = this._db.LoadDocuments(MONGO_MODELS.USER, filter).FirstOrDefault();

            user.Username = edit.Name;
            user.PhoneNumber = edit.PhoneNumber;
            this._db.UpdateDocument(MONGO_MODELS.USER, user);
            response.IsSuccess = true;
            response.Error = null;


        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.Error = ERROR_SETTINGS.USER_NOT_FOUND;
            this._logger.LogError("USERCONTROLLER", "EditProfile", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
        }

        return response;


    }


    private FacilityResponse edituserprofile(Edituserprofilerequest request)
    {
        var response = new FacilityResponse();
        try
        {
            var filter = Builders<MDBL_User>.Filter.Eq(user => user.UserId, _authenticationService.User.UserId);
            var user = this._db.LoadDocuments(MONGO_MODELS.USER, filter).FirstOrDefault();
            user.Username = request.Name;
            user.Location = request.Location;
            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedDate = DateTime.UtcNow;
            this._db.UpdateDocument(MONGO_MODELS.USER, user);
            response.IsSuccess = true;
            response.Error = null;

        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.Error = ERROR_SETTINGS.USER_NOT_FOUND;
            this._logger.LogError("USERCONTROLLER", "EdituserProfile", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());

        }
        return response;
    }


    private ActionResult<FacilityResponse> editTask(EditTaskRequest request)
    {
        var response = new FacilityResponse();
        try
        {
            var taskfilter = Builders<MDBL_TASk>.Filter.Eq(task => task.TaskId, request.TaskId);
            var task = this._db.LoadDocuments(MONGO_MODELS.TASK, taskfilter).FirstOrDefault();

            task.Status = request.TaskStatus;
            task.remarks = request.Remarks;

            this._db.UpdateDocument(MONGO_MODELS.TASK, task);

            response.IsSuccess = true;
            response.Error = null;

        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.Error = ERROR_SETTINGS.TASK_FAILURE;
            this._logger.LogError("USERCONTROLLER", "EditTask", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());


        }


        return response;
    }


    private TaskDetails getalltask()
    {
        var response = new TaskDetails();

        try
        {
            List<TaskView> tasks = new List<TaskView>();
            var taskcount = this._db.LoadAll<MDBL_TASk>(MONGO_MODELS.TASK).Count;

            var taskfilter = Builders<MDBL_TASk>.Filter.Empty;
            var task = this._db.LoadDocuments(MONGO_MODELS.TASK, taskfilter).ToList();
            if (task != null)
            {
                foreach (var n in task)
                {
                    var ntask = new TaskView();
                    ntask.TaskId = n.TaskId;
                    ntask.Concept = n.Concept;
                    ntask.Location = n.Location;
                    ntask.Persontocontact = n.Persontocontact;
                    ntask.Responsibility = n.Responsibility;
                    ntask.Priority = n.Priority;
                    ntask.Status = n.Status;
                    ntask.ConcernRaisedDate = n.ConcernRaisedDate;
                    ntask.RaisedTime = n.RaisedTime;
                    ntask.Aging = n.Aging;
                    ntask.ActionPlan = n.ActionPlan;
                    ntask.ApprovedQuotationDate = n.ApprovedQuotationDate;
                    ntask.MaintainenceWork = n.MaintainenceWork;
                    tasks.Add(ntask);


                }
            }

        }
        catch (Exception ex)
        {
            this._logger.LogError("USERCONTROLLER", "GetAllTask", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());

        }

        return response;
    }



    private List<TaskView> GetPendingtasks()
    {
        var response = new List<TaskView>();
        try
        {
            var taskfilter = Builders<MDBL_TASk>.Filter.Where(task => task.Status == "PENDING");
            var task = this._db.LoadDocuments(MONGO_MODELS.TASK, taskfilter).ToList();

            foreach (var n in task)
            {
                var ntask = new TaskView();
                ntask.TaskId = n.TaskId;
                ntask.Concept = n.Concept;
                ntask.Location = n.Location;
                ntask.Persontocontact = n.Persontocontact;
                ntask.Responsibility = n.Responsibility;
                ntask.Priority = n.Priority;
                ntask.Status = n.Status;
                ntask.ConcernRaisedDate = n.ConcernRaisedDate;
                ntask.RaisedTime = n.RaisedTime;
                ntask.Aging = n.Aging;
                ntask.ActionPlan = n.ActionPlan;
                ntask.ApprovedQuotationDate = n.ApprovedQuotationDate;
                ntask.MaintainenceWork = n.MaintainenceWork;
                response.Add(ntask);
            }
        }
        catch (Exception ex) { this._logger.LogError("USERCONTROLLER", "GetAllTask", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString()); }

        return response;
    }


    private List<TaskView> GetCompletedtasks()
    {
        var response = new List<TaskView>();
        try
        {
            var taskfilter = Builders<MDBL_TASk>.Filter.Where(task => task.Status == "COMPLETED");
            var task = this._db.LoadDocuments(MONGO_MODELS.TASK, taskfilter).ToList();

            foreach (var n in task)
            {
                var ntask = new TaskView();
                ntask.TaskId = n.TaskId;
                ntask.Concept = n.Concept;
                ntask.Location = n.Location;
                ntask.Persontocontact = n.Persontocontact;
                ntask.Responsibility = n.Responsibility;
                ntask.Priority = n.Priority;
                ntask.Status = n.Status;
                ntask.ConcernRaisedDate = n.ConcernRaisedDate;
                ntask.RaisedTime = n.RaisedTime;
                ntask.Aging = n.Aging;
                ntask.ActionPlan = n.ActionPlan;
                ntask.ApprovedQuotationDate = n.ApprovedQuotationDate;
                ntask.MaintainenceWork = n.MaintainenceWork;
                response.Add(ntask);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError("USERCONTROLLER", "GetCompletedtasks", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
        }
        return response;
    }

    private List<GetUserResponse> getAllUsers()
    {
        var response = new List<GetUserResponse>();
        try
        {
            var userfilter = Builders<MDBL_User>.Filter.Empty;
            var user = this._db.LoadDocuments(MONGO_MODELS.USER, userfilter).ToList();

            foreach (var n in user)
            {
                var newuser = new GetUserResponse();
                newuser.Username = n.Username;
                newuser.PhoneNumber = n.PhoneNumber;
                newuser.Email = n.Email;
                newuser.Location = n.Location;
                newuser.UserType = n.UserType;

                response.Add(newuser);

            }
        }
        catch (Exception ex) { this._logger.LogError("USERCONTROLLER", "GetCompletedtasks", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString()); }
        return response;
    }


    private TotalCountView viewCount(string UserType)
    {
        var response = new TotalCountView();

        try
        {
            if (UserType == ACCOUNTS_USERTYPE_MASTER.ADMIN || UserType == ACCOUNTS_USERTYPE_MASTER.MASTER)
            {
                var userfilter = Builders<MDBL_User>.Filter.Empty;
                response.TotalUser = this._db.LoadDocuments(MONGO_MODELS.USER, userfilter).Count();
                var clientfilter = Builders<MDBL_User>.Filter.Eq(user => user.UserType, ACCOUNTS_USERTYPE_MASTER.CLIENT);
                response.TotalClient = this._db.LoadDocuments(MONGO_MODELS.USER, clientfilter).Count();
                var adminfilter = Builders<MDBL_User>.Filter.Eq(user => user.UserType, ACCOUNTS_USERTYPE_MASTER.ADMIN);
                response.TotalAdmin = this._db.LoadDocuments(MONGO_MODELS.USER, clientfilter).Count();

            }
            var taskfilter = Builders<MDBL_TASk>.Filter.Empty;
            response.TotalTask = this._db.LoadDocuments(MONGO_MODELS.TASK, taskfilter).Count();
            var pendingtaskfilter = Builders<MDBL_TASk>.Filter.Eq(task => task.Status, "PENDING");
            response.TotalTask = this._db.LoadDocuments(MONGO_MODELS.TASK, pendingtaskfilter).Count();
            var completedtaskfilter = Builders<MDBL_TASk>.Filter.Eq(task => task.Status, "COMPLETED");
            response.CompletedTask = this._db.LoadDocuments(MONGO_MODELS.TASK, completedtaskfilter).Count();
        }
        catch (Exception ex)
        {
            this._logger.LogError("USERCONTROLLER", "ViewCount", $"{ex.Message}", this.HttpContext.Connection.RemoteIpAddress.ToString());
        }

        return response;

    }

    #endregion
}