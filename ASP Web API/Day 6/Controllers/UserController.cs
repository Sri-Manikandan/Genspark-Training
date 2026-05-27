using ExcelAPI.Interfaces;
using ExcelAPI.Models;
using Microsoft.AspNetCore.Mvc;
using ClosedXML.Excel;

namespace ExcelAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportUsersToExcel()
        {
            var users = await _userService.GetAllUsers();

            using var workbook = new XLWorkbook();
            var sheet = workbook.Worksheets.Add("Users");

            // Add headers
            sheet.Cell(1, 1).Value = "ID";
            sheet.Cell(1, 2).Value = "Name";
            sheet.Cell(1, 3).Value = "Email";
            sheet.Cell(1, 4).Value = "Phone";

            int row = 2;
            foreach (var user in users)
            {
                sheet.Cell(row, 1).Value = user.Id;
                sheet.Cell(row, 2).Value = user.Name;
                sheet.Cell(row, 3).Value = user.Email;
                sheet.Cell(row, 4).Value = user.Phone;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Users.xlsx");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _userService.GetAllUsers();
            if (users == null || !users.Any())
            {
                return NotFound();
            }
            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<User?>> GetUserById(int id)
        {
            var user = await _userService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<User>> CreateUser(User user)
        {
            var createdUser = await _userService.CreateUser(user);
            if(user.Phone.Length != 10)
            {
                _logger.LogWarning("Attempted to create user with invalid phone number: {Phone}", user.Phone);
                return BadRequest("Phone number must be 10 digits long.");
            }
            return CreatedAtAction(nameof(GetUserById), new { id = createdUser.Id }, createdUser);
        }

        [HttpPut]
        public async Task<ActionResult<User>> UpdateUser(User user)
        {
            var updatedUser = await _userService.UpdateUser(user);
            return Ok(updatedUser);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            await _userService.DeleteUser(id);
            return NoContent();
        }
    }
}