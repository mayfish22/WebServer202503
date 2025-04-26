using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebServer.Models.ViewModels;
using WebServer.Models.WebServerDB;

namespace WebServer.Controllers;

[Authorize]
[Route("{controller}/{action=Index}")]
public class EmployeeController : Controller
{
    private readonly WebServerDBContext _webServerDB;

    public EmployeeController(WebServerDBContext webServerDB)
    {
        _webServerDB = webServerDB;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        await Task.Yield();
        return View("~/Views/Employee/Index.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> GetData(int draw, int start, int length)
    {
        try
        {
            var query = from e in _webServerDB.Employee
                        select e;

            var recordsTotal = await query.CountAsync();

            if (!string.IsNullOrEmpty((string)Request.Form["search[value]"]))
            {
                string sQuery = Request.Form["search[value]"].ToString().ToUpper();
                query = query.Where(e => e.No.ToUpper().Contains(sQuery) ||
                                         e.Name.ToUpper().Contains(sQuery) ||
                                         e.Email.ToUpper().Contains(sQuery) ||
                                         e.Mobile.ToUpper().Contains(sQuery));
            }

            int sortColumnIndex = (string)Request.Form["order[0][column]"] == null ? -1 : int.Parse(Request.Form["order[0][column]"]);
            string sortDirection = (string)Request.Form["order[0][dir]"] == null ? "" : Request.Form["order[0][dir]"].ToString().ToUpper();
            string sortColumn = Request.Form["columns[" + sortColumnIndex + "][data]"].ToString() ?? "";

            bool bDescending = sortDirection.Equals("DESC");
            switch (sortColumn)
            {
                case "no":
                    query = bDescending ? query.OrderByDescending(e => e.No) : query.OrderBy(e => e.No);
                    break;
                case "name":
                    query = bDescending ? query.OrderByDescending(e => e.Name) : query.OrderBy(e => e.Name);
                    break;
                case "email":
                    query = bDescending ? query.OrderByDescending(e => e.Email) : query.OrderBy(e => e.Email);
                    break;
                case "mobile":
                    query = bDescending ? query.OrderByDescending(e => e.Mobile) : query.OrderBy(e => e.Mobile);
                    break;
                default:
                    query = query.OrderBy(e => e.No);
                    break;
            }

            var recordsFiltered = await query.CountAsync();
            var list = recordsFiltered == 0
                ? new List<Employee>()
                : query.Skip(start).Take(Math.Min(length, recordsFiltered - start)).ToList();

            list.ForEach(s =>
            {
                s.PhotoURL = $"/FileStorage/Download/{s.Photo}";
                s.FaceFeatureURL = $"/FileStorage/Download/{s.FaceFeature}";
            });

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
            Log.Error(e, $"{nameof(EmployeeController)}.{nameof(GetData)}");

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

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = await GetEmployeeViewModelAsync(null, false);
        return View("~/Views/Employee/Default.cshtml", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeViewModel model)
    {
        try
        {
            model.IsReadonly = false;

            if (!ModelState.IsValid)
                return View("~/Views/Employee/Default.cshtml", model);

            var employee = model.Employee;
            employee.No = employee.No.Trim().ToUpper();
            employee.Name = employee.Name.Trim();
            employee.Email = employee.Email?.Trim().ToUpper();
            employee.Mobile = employee.Mobile?.Trim();
            employee.CreatedDT = DateTime.Now;

            await _webServerDB.Employee.AddAsync(employee);
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            ModelState.AddModelError(nameof(EmployeeViewModel.ErrorMessage), e.Message);
            Log.Error(e, $"{nameof(EmployeeController)}.{nameof(Create)}");
            return View("~/Views/Employee/Default.cshtml", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Detail(Guid id)
    {
        var model = await GetEmployeeViewModelAsync(id, true);
        return View("~/Views/Employee/Default.cshtml", model);
    }

    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await GetEmployeeViewModelAsync(id, false);
        return View("~/Views/Employee/Default.cshtml", model);
    }

    [HttpPost("{id:Guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EmployeeViewModel model)
    {
        try
        {
            model.IsReadonly = false;

            if (!ModelState.IsValid)
                return View("~/Views/Employee/Default.cshtml", model);

            var employee = await _webServerDB.Employee.FindAsync(model.Employee.ID);
            if( employee == null)
                throw new Exception("查無此員工");
            employee.No = model.Employee.No?.Trim().ToUpper();
            employee.Name = model.Employee.Name.Trim();
            employee.Email = model.Employee.Email?.Trim().ToUpper();
            employee.Mobile = model.Employee.Mobile.Trim();
            employee.HireDate = model.Employee.HireDate;
            employee.Photo = model.Employee.Photo;
            employee.FaceFeature = model.Employee.FaceFeature;
            employee.ModifiedDT = DateTime.Now;

            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            ModelState.AddModelError(nameof(EmployeeViewModel.ErrorMessage), e.Message);
            Log.Error(e, $"{nameof(EmployeeController)}.{nameof(Edit)}");
            return View("~/Views/Employee/Default.cshtml", model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:Guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var model = await GetEmployeeViewModelAsync(id, true);
        return View("~/Views/Employee/Default.cshtml", model);
    }

    [HttpPost("{id:Guid}"), ActionName(nameof(Delete))]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        try
        {
            var employee = await _webServerDB.Employee.FindAsync(id);
            if (employee == null)
                throw new Exception("查無此員工");
            _webServerDB.Employee.Remove(employee);
            await _webServerDB.SaveChangesAsync();
        }
        catch (Exception e)
        {
            var model = await GetEmployeeViewModelAsync(id, true);
            ModelState.AddModelError(nameof(EmployeeViewModel.ErrorMessage), e.Message);
            Log.Error(e, $"{nameof(EmployeeController)}.{nameof(DeleteConfirmed)}");
            return View("~/Views/Employee/Default.cshtml", model);
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<EmployeeViewModel> GetEmployeeViewModelAsync(Guid? id, bool isReadonly)
    {
        if (id.HasValue)
        {
            var employee = await _webServerDB.Employee.FindAsync(id);

            if (employee == null)
                throw new Exception("查無此員工");

            if (employee.Photo.HasValue)
                employee.PhotoURL = $"/FileStorage/Download/{employee.Photo}";

            return new EmployeeViewModel
            {
                Employee = employee,
                IsReadonly = isReadonly,
            };
        }
        else
        {
            return new EmployeeViewModel
            {
                Employee = new Employee
                {
                    ID = Guid.NewGuid(),
                    HireDate = DateOnly.FromDateTime(DateTime.Now), // 預設為當日
                },
                IsReadonly = isReadonly,
            };
        }
    }
}