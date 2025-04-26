using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebServer.Hubs;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers;

[Authorize]
[Route("{controller}/{action=Index}")]
public class ClockRecordController : Controller
{
    private readonly WebServerDBContext _webServerDB;
    private readonly IHubContext<FaceHub> _faceHub;

    public ClockRecordController(WebServerDBContext webServerDB, IHubContext<FaceHub> faceHub)
    {
        _webServerDB = webServerDB;
        _faceHub = faceHub;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await Task.Yield();
        return View("~/Views/ClockRecord/Index.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> GetData(int draw, int start, int length)
    {
        try
        {
            var query = from n1 in _webServerDB.ClockRecord
                        join n2 in _webServerDB.Employee on n1.EmployeeID equals n2.ID
                        select new ClockRecord {
                            ID = n1.ID,
                            EmployeeID = n1.EmployeeID,
                            ClockDateTime = n1.ClockDateTime,
                            Type = n1.Type,
                            Location = n1.Location,
                            CreatedDT = n1.CreatedDT,
                            EmployeeNo = n2.No,
                            EmployeeName = n2.Name,
                        };

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrEmpty((string)Request.Form["search[value]"]))
            {
                string sQuery = Request.Form["search[value]"].ToString().ToUpper();
                query = query.Where(e => (!string.IsNullOrEmpty(e.Type) && e.Type.ToUpper().Contains(sQuery)) ||
                                        (!string.IsNullOrEmpty(e.Location) && e.Location.ToUpper().Contains(sQuery)) ||
                                        (!string.IsNullOrEmpty(e.EmployeeNo) && e.EmployeeNo.ToUpper().Contains(sQuery)) ||
                                        (!string.IsNullOrEmpty(e.EmployeeName) && e.EmployeeName.ToUpper().Contains(sQuery))
                                    );
            }

            int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
            string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
            string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

            bool bDescending = sortDirection.Equals("DESC");
            switch (sortColumn)
            {
                case "type":
                    query = bDescending ? query.OrderByDescending(e => e.Type) : query.OrderBy(e => e.Type);
                    break;
                case "location":
                    query = bDescending ? query.OrderByDescending(e => e.Location) : query.OrderBy(e => e.Location);
                    break;
                case "employeeNo":
                    query = bDescending ? query.OrderByDescending(e => e.EmployeeNo) : query.OrderBy(e => e.EmployeeNo);
                    break;
                case "employeeName":
                    query = bDescending ? query.OrderByDescending(e => e.EmployeeName) : query.OrderBy(e => e.EmployeeName);
                    break;
                case "clockDateTime":
                    query = bDescending ? query.OrderByDescending(e => e.ClockDateTime) : query.OrderBy(e => e.ClockDateTime);
                    break;
                case "createdDT":
                    query = bDescending ? query.OrderByDescending(e => e.CreatedDT) : query.OrderBy(e => e.CreatedDT);
                    break;
                default:
                    query = query.OrderByDescending(e => e.CreatedDT);
                    break;
            }

            var recordsFiltered = await query.CountAsync();
            var list = recordsFiltered == 0
                ? new List<ClockRecord>()
                : query.Skip(start).Take(Math.Min(length, recordsFiltered - start)).ToList();

            dynamic dataTableData = new
            {
                draw = draw,
                data = list,
                recordsTotal = recordsTotal,
                recordsFiltered = recordsFiltered,
            };

            return Json(dataTableData);
        }
        catch (Exception e)
        {
            Log.Error(e, $"{nameof(ClockRecordController)}.{nameof(GetData)}");

            dynamic dataTableData = new
            {
                draw = draw,
                data = Array.Empty<string>(),
                recordsTotal = 0,
                recordsFiltered = 0,
                errorMessage = e.Message,
            };

            return Json(dataTableData);
        }
    }

    /// <summary>
    /// 取得員工的特徵檔
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> EmployeeFFs()
    {
        try
        {
            var ffs = await (from n1 in _webServerDB.Employee
                             orderby n1.No
                             select new
                             {
                                 id = n1.ID,
                                 no = n1.No,
                                 name = n1.Name,
                                 ffId = n1.FaceFeature,
                             }).ToListAsync();
            return Json(ffs);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"{nameof(ClockRecordController)}.{nameof(EmployeeFFs)}");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 新增打卡紀錄
    /// </summary>
    /// <param name="location"></param>
    /// <param name="type"></param>
    /// <param name="employeeNo"></param>
    /// <returns></returns>
    [HttpGet("{location}/{type}/{employeeNo}")]
    [AllowAnonymous]
    public async Task<IActionResult> AddRecord(string location, string type, string employeeNo)
    {
        try
        {
            var employee = await _webServerDB.Employee.FirstOrDefaultAsync(e => e.No == employeeNo);
            if(employee == null)
            {
                return BadRequest($"查無員工【{employeeNo}】");
            }

            var clockRecord = new ClockRecord
            {
                ID = Guid.NewGuid(),
                EmployeeID = employee.ID,
                ClockDateTime = DateTime.Now,
                Type = type,
                Location = location,
                CreatedDT = DateTime.Now,
            };
            await _webServerDB.ClockRecord.AddAsync(clockRecord);
            await _webServerDB.SaveChangesAsync();
            // 推送ID到前端
            await _faceHub.Clients.All.SendAsync("ReceiveClockRecord", clockRecord.ID.ToString());
            return Ok(clockRecord.ID);
        }
        catch(Exception ex)
        {
            Log.Error(ex, $"{nameof(ClockRecordController)}.{nameof(AddRecord)}");
            return BadRequest(ex.Message);
        }
    }
}