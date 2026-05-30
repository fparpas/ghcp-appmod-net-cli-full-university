using System;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using ContosoUniversity.Data;
using ContosoUniversity.Models;
using ContosoUniversity.Services;

namespace ContosoUniversity.Controllers
{
    public class CoursesController : BaseController
    {
        private readonly IBlobStorageService _blobStorageService;

        public CoursesController(SchoolContext context, NotificationService notificationSvc, IBlobStorageService blobStorageService)
            : base(context, notificationSvc)
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
                return BadRequest();
            Course course = db.Courses.Include(c => c.Department).Where(c => c.CourseID == id).Single();
            if (course == null)
                return NotFound();
            return View(course);
        }

        // GET: Courses/Create
        public IActionResult Create()
        {
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name");
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
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    if (teachingMaterialImage.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "File size must be less than 5MB.");
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    try
                    {
                        var blobName = $"course_{course.CourseID}_{Guid.NewGuid()}{fileExtension}";
                        using var stream = teachingMaterialImage.OpenReadStream();
                        course.TeachingMaterialImagePath = await _blobStorageService.UploadFileAsync(
                            stream, blobName, teachingMaterialImage.ContentType);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Error uploading file: " + ex.Message);
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }
                }

                db.Courses.Add(course);
                db.SaveChanges();

                SendEntityNotification("Course", course.CourseID.ToString(), course.Title, EntityOperation.CREATE);

                return RedirectToAction("Index");
            }

            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
            return View(course);
        }

        // GET: Courses/Edit/5
        public IActionResult Edit(int? id)
        {
            if (id == null)
                return BadRequest();
            Course course = db.Courses.Find(id);
            if (course == null)
                return NotFound();
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
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
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    if (teachingMaterialImage.Length > 5 * 1024 * 1024)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "File size must be less than 5MB.");
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }

                    try
                    {
                        // Delete the old blob (if any) before uploading the replacement.
                        if (!string.IsNullOrEmpty(course.TeachingMaterialImagePath))
                        {
                            await _blobStorageService.DeleteFileAsync(course.TeachingMaterialImagePath);
                        }

                        var blobName = $"course_{course.CourseID}_{Guid.NewGuid()}{fileExtension}";
                        using var stream = teachingMaterialImage.OpenReadStream();
                        course.TeachingMaterialImagePath = await _blobStorageService.UploadFileAsync(
                            stream, blobName, teachingMaterialImage.ContentType);
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("teachingMaterialImage", "Error uploading file: " + ex.Message);
                        ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
                        return View(course);
                    }
                }

                db.Entry(course).State = EntityState.Modified;
                db.SaveChanges();

                SendEntityNotification("Course", course.CourseID.ToString(), course.Title, EntityOperation.UPDATE);

                return RedirectToAction("Index");
            }
            ViewBag.DepartmentID = new SelectList(db.Departments, "DepartmentID", "Name", course.DepartmentID);
            return View(course);
        }

        // GET: Courses/Delete/5
        public IActionResult Delete(int? id)
        {
            if (id == null)
                return BadRequest();
            Course course = db.Courses.Include(c => c.Department).Where(c => c.CourseID == id).Single();
            if (course == null)
                return NotFound();
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Course course = db.Courses.Find(id);
            var courseTitle = course.Title;

            // Delete the teaching-material blob from Azure Blob Storage (if one exists).
            if (!string.IsNullOrEmpty(course.TeachingMaterialImagePath))
            {
                await _blobStorageService.DeleteFileAsync(course.TeachingMaterialImagePath);
            }

            db.Courses.Remove(course);
            db.SaveChanges();

            SendEntityNotification("Course", id.ToString(), courseTitle, EntityOperation.DELETE);

            return RedirectToAction("Index");
        }
    }
}

