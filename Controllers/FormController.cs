
using Microsoft.AspNetCore.Mvc;
using WallsShop.DTO;
using WallsShop.Repository;

namespace WallsShop.Controllers;

[ApiController]
[Route("api/[controller]")]
// التعديل 1: الوراثة من ControllerBase
public class FormController(FormRepository repo) : ControllerBase
{
    [HttpPost("add-form")]
    public async Task<IActionResult> Create([FromBody] FormDto form)
    {
        // التعديل 2: الاعتماد على الـ bool الذي يرجع من الـ Repo مباشرة
        var isSuccess = await repo.CreateForm(form);

        if (isSuccess)
        {
            // التعديل 3: إرجاع JSON متوافق تماماً مع Angular
            return Ok(new { message = "Form submitted successfully." });
        }

        // التعديل 4: إرجاع حالة 400 في حال الفشل
        return BadRequest(new { message = "Failed to submit form. Please try again later." });
    }
}
//using Microsoft.AspNetCore.Mvc;
//using WallsShop.DTO;
//using WallsShop.Repository;

//namespace WallsShop.Controllers;

//[ApiController]
//[Route("api/[controller]")]
//public class FormController(FormRepository repo) : Controller
//{
//    [HttpPost("add-form")]
//    public async Task<IActionResult> Create([FromBody] FormDto form)
//    {
//        try
//        {
//            var result = await repo.CreateForm(form);
//            return Ok(result ? "Form submitted successfully." : "Failed to submit form.");
//        }
//        catch (Exception e)
//        {
//            return BadRequest(e.Message);
//        }
//    }
//}