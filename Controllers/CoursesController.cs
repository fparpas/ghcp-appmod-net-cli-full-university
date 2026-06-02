using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.AspNetCore.Http;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers
{
    public class CoursesController : BaseController
    {
        private readonly IBlobStorageService _blobStorageService;

        public CoursesController(SchoolContext db, NotificationService notificationService, IBlobStorageService blobStorageService)
            : base(db, notificationService)
        {
            _blobStorageService = blobStorageService;
        }

        // GET: Courses
        public IActionResult Index()
        {
            var courses = db.Courses.Include(c => c.Department);
            return View(courses.ToList());
        }

        // GET: Courses/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Course course = db.Courses.Include(c => c.Department).Where(c => c.CourseID == id).Single();
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name");
            return View(new Course());
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CourseID,Title,Credits,DepartmentID,TeachingMaterialImagePath")] Course course, IFormFile teachingMaterialImage)
        {
            if (ModelState.IsValid)
            {
                if (teachingMaterialImage != null && teachingMaterialImage.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var fileExtension = Path.GetExtension(teachingMaterialImage.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Please upload a valid image file (jpg, jpeg, png, gif, bmp).");
                        ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    if (teachingMaterialImage.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "File size must be less than 5MB.");
                        ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    try
                    {
                        var fileName = "course_" + course.CourseID + "_" + Guid.NewGuid() + fileExtension;
                        var contentType = GetContentType(fileExtension);

                        using (var stream = teachingMaterialImage.OpenReadStream())
                        {
                            course.TeachingMaterialImagePath = await _blobStorageService.UploadAsync(
                                stream, fileName, contentType);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Error uploading file: " + ex.Message);
                        ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }
                }

                db.Courses.Add(course);
                db.SaveChanges();

                await SendEntityNotificationAsync("Course", course.CourseID.ToString(), course.Title, EntityOperation.CREATE);

                return RedirectToAction("Index");
            }

            ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
            return View(course);
        }

        // GET: Courses/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return NotFound();
            }
            ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
            return View(course);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("CourseID,Title,Credits,DepartmentID,TeachingMaterialImagePath")] Course course, IFormFile teachingMaterialImage)
        {
            if (ModelState.IsValid)
            {
                if (teachingMaterialImage != null && teachingMaterialImage.Length > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
                    var fileExtension = Path.GetExtension(teachingMaterialImage.FileName).ToLower();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Please upload a valid image file (jpg, jpeg, png, gif, bmp).");
                        ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    if (teachingMaterialImage.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "File size must be less than 5MB.");
                        ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    try
                    {
                        // Delete the old blob before uploading the replacement.
                        // DeleteAsync handles null/empty paths and legacy local paths gracefully.
                        await _blobStorageService.DeleteAsync(course.TeachingMaterialImagePath);

                        var fileName = "course_" + course.CourseID + "_" + Guid.NewGuid() + fileExtension;
                        var contentType = GetContentType(fileExtension);

                        using (var stream = teachingMaterialImage.OpenReadStream())
                        {
                            course.TeachingMaterialImagePath = await _blobStorageService.UploadAsync(
                                stream, fileName, contentType);
                        }
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Error uploading file: " + ex.Message);
                        ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }
                }

                db.Entry(course).State = EntityState.Modified;
                db.SaveChanges();

                await SendEntityNotificationAsync("Course", course.CourseID.ToString(), course.Title, EntityOperation.UPDATE);

                return RedirectToAction("Index");
            }
            ViewBag.DepartmentID = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
            return View(course);
        }

        // GET: Courses/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }
            Course course = db.Courses.Include(c => c.Department).Where(c => c.CourseID == id).Single();
            if (course == null)
            {
                return NotFound();
            }
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Course course = db.Courses.Find(id);
            var courseTitle = course.Title;

            // Delete the associated teaching material blob from Azure Blob Storage.
            // DeleteAsync handles null/empty paths and legacy local paths gracefully.
            await _blobStorageService.DeleteAsync(course.TeachingMaterialImagePath);

            db.Courses.Remove(course);
            db.SaveChanges();

            await SendEntityNotificationAsync("Course", id.ToString(), courseTitle, EntityOperation.DELETE);

            return RedirectToAction("Index");
        }

        // ── Private helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Maps a normalised image file extension to its MIME content-type string.
        /// </summary>
        private static string GetContentType(string fileExtension)
        {
            switch (fileExtension)
            {
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                default:
                    return "application/octet-stream";
            }
        }
    }
}
