﻿using DataAccess.Logic;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SoftwareTechnologyCalendarApplication.Models;
using SoftwareTechnologyCalendarApplicationMVC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SoftwareTechnologyCalendarApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUserDataAccess UserDataAccess;
        private readonly ICalendarDataAccess CalendarDataAccess;
        private readonly IEventDataAccess EventDataAccess;
        //private string UserName="";

        public HomeController(ILogger<HomeController> logger, IUserDataAccess userDataAccess, 
            ICalendarDataAccess calendarDataAccess, IEventDataAccess eventDataAccess)
        {
            UserDataAccess = userDataAccess;
            CalendarDataAccess = calendarDataAccess;
            EventDataAccess = eventDataAccess;
            _logger = logger;
        }

        public IActionResult AddCalendar()
        {
            AuthorizeUser();

            ViewData["DuplicateCalendarTitle"] = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCalendar(Models.Calendar calendar)
        {
            AuthorizeUser();

            ViewData["DuplicateCalendarTitle"] = false;
            if (!ModelState.IsValid)
            {
                return View();
            }
            UserDataModel user = UserDataAccess.GetUser(ActiveUser.User.Username);
            foreach(CalendarDataModel calendarDataModelTemp in user.Calendars)
            {
                if (calendarDataModelTemp.Title == calendar.Title)
                {
                    ViewData["DuplicateCalendarTitle"] = true;
                    return View();
                };
            }

            CalendarDataModel calendarDataModel = new CalendarDataModel();
            calendarDataModel.Title = calendar.Title;
            //add the categories the user wrote in the textarea
            if(calendar.Categories.First() != null)
            {
                calendarDataModel.Categories = calendar.Categories;
            }

            //add the categories that the user checked in the checkbox area
            IEnumerable<string> selectedCategories = Request.Form["SelectedCategories"];
            foreach (string category in selectedCategories)
            {
                calendarDataModel.Categories.Add(category);
            }

            CalendarDataAccess.CreateCalendar(calendarDataModel, user.Username);
            return RedirectToAction("HomePage", "Home", new {pagination = 1 });
        }

        public IActionResult HomePage(int pagination, bool calendarWasDeleted)
        {
            AuthorizeUser();

            UserDataModel userDataModel = UserDataAccess.GetUser(ActiveUser.User.Username);
            User user = new User(userDataModel);
            ViewData["DeletedCalendar"] = calendarWasDeleted;
            ViewData["pagination"] = pagination * 6;
            return View(user);
        }

        public IActionResult DeleteCalendar(int calendarId)
        {
            AuthorizeUser();

            CalendarDataAccess.DeleteCalendar(calendarId);
            return RedirectToAction("HomePage", "Home", new { pagination = 1 ,
                calendarWasDeleted = true});
        }

        [HttpPost]
        public IActionResult DeleteEvent(int calendarId,int eventId,int year,int month, int day)
        {
            AuthorizeUser();

            EventDataAccess.DeleteEvent(eventId);
            return RedirectToAction("ViewCalendarDay", "Home", new
            {
                username = ActiveUser.User.Username,
                calendarId = calendarId,
                year = year,
                month = month,
                day = day,
                eventWasDeleted = true
            });
        }

        public IActionResult ViewCalendar(int calendarId, int month, int year)
        {
            AuthorizeUser();

            int monthIndex = month == 0? DateTime.Today.Month : month;
            int yearIndex = year == 0 ? DateTime.Today.Year : year; 

            int monthlength;
            if (monthIndex == 1 || monthIndex == 3 ||
                monthIndex == 5 || monthIndex == 7 ||
                monthIndex == 8 || monthIndex == 10 ||
                monthIndex == 12)
            {
                monthlength = 31;
            }
            else if (monthIndex == 4 || monthIndex == 6 ||
                monthIndex == 9 || monthIndex == 11)
            {
                monthlength = 30;
            }
            else if (yearIndex % 4 != 0 && monthIndex == 2)
            {
                monthlength = 28;
            }
            else
            {
                monthlength = 29;
            }

            //get the offset (numerical represantation of the day- for example Monday = 0, Tuesday = 1 ...)
            //adjusted by one, because it starts as Sunday = 0 and We are going with monday = 0;
            int offset = new DateTime(yearIndex, monthIndex, 1).DayOfWeek != 0 ? (int)new DateTime(yearIndex, monthIndex, 1).DayOfWeek - 1 : 6;

            CalendarDataModel calendarDataModel = CalendarDataAccess.GetCalendar(calendarId);
            Models.Calendar calendar = new Models.Calendar(calendarDataModel);
            ViewData["MonthLength"] = monthlength;
            ViewData["Offset"] = offset;
            ViewData["MonthName"] = CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(monthIndex);
            ViewData["Month"] = monthIndex;
            ViewData["Year"] = yearIndex; 
            return View(calendar);
        }

        public IActionResult ViewCalendarDay(int calendarId, int month, int year, int day, bool eventWasDeleted)
        {
            AuthorizeUser();

            CalendarDataModel calendarDataModel = CalendarDataAccess.GetCalendar(calendarId);
            Models.Calendar calendar = new Models.Calendar(calendarDataModel);
            ViewData["Month"] = month;
            ViewData["Year"] = year;
            ViewData["Day"] = day;
            ViewData["EventWasDeleted"] = eventWasDeleted;
            return View(calendar);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult editEvent(int eventId)
        {
            AuthorizeUser();

            ViewData["DuplicateEventTitle"] = false;
            ViewData["Editing"] = true;
            ViewData["EventId"] = eventId;
            EventDataModel eventDataModelTemp = EventDataAccess.GetEvent(eventId);
            Event eventt = new Event();
            eventt.Id=eventDataModelTemp.Id;
            eventt.Description = eventDataModelTemp.Description;
            eventt.Title = eventDataModelTemp.Title;
            eventt.StartingTime = eventDataModelTemp.StartingTime;
            eventt.EndingTime = eventDataModelTemp.EndingTime;
            eventt.AlertStatus = eventDataModelTemp.AlertStatus;
            return View(eventt);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult editEvent(int eventId, Event eventt)
        {
            AuthorizeUser();

            if (!ModelState.IsValid)
            {
                return View();
            }
            EventDataModel eventDataModel = new EventDataModel();
            eventDataModel.Id = eventId;
            eventDataModel.Title = eventt.Title;
            eventDataModel.Description = eventt.Description;
            eventDataModel.StartingTime = eventt.StartingTime;
            eventDataModel.EndingTime = eventt.EndingTime;
            eventDataModel.AlertStatus = eventt.AlertStatus;
            EventDataAccess.UpdateEvent(eventDataModel);
            return RedirectToAction("HomePage", "Home", new { pagination = 1 });
        }

        public IActionResult addEvent(int calendarId)
        {
            AuthorizeUser();

            ViewData["DuplicateEventTitle"] = false;
            ViewData["CalendarId"] = calendarId;
            ViewData["Editing"] = false;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult addEvent(int calendarId, Event eventt)
        {
            AuthorizeUser();

            ViewData["DuplicateEventTitle"] = false;
            if (!ModelState.IsValid)
            {
                return View();
            }

            List < EventDataModel > eventList = EventDataAccess.GetEvents(calendarId);
            foreach (EventDataModel eventDataModelTemp in eventList)
            {
                if(eventDataModelTemp.Title == eventt.Title)
                {
                    ViewData["DuplicateEventTitle"] = true;
                    return View();
                }
            }
            //username = Request("username").toString();
            EventDataModel eventDataModel = new EventDataModel();
            eventDataModel.Id = eventt.Id;
            eventDataModel.Title = eventt.Title;
            eventDataModel.Description = eventt.Description;
            eventDataModel.StartingTime = eventt.StartingTime;
            eventDataModel.EndingTime = eventt.EndingTime;
            eventDataModel.AlertStatus = eventt.AlertStatus;

            EventDataAccess.CreateEvent(eventDataModel, ActiveUser.User.Username, calendarId);
            return RedirectToAction("HomePage", "Home", new {pagination = 1 });
        }
        
        public IActionResult ViewNotifications(string username)
        {
            AuthorizeUser();
            throw new NotImplementedException();
        }

        private static void AuthorizeUser()
        {
            if (ActiveUser.User == null)
            {
                throw new NotImplementedException();
            }
        }
        public IActionResult editAccount(string username)
        {
            ViewData["DuplicateEventTitle"] = false;
            ViewData["User"] = username;
            UserDataModel userDataModelTemp = UserDataAccess.GetUser(username);
            User userr = new User(userDataModelTemp);
            //eventt.Id = eventDataModelTemp.Id;
            //eventt.Description = eventDataModelTemp.Description;
            //eventt.Title = eventDataModelTemp.Title;
            //eventt.StartingTime = eventDataModelTemp.StartingTime;
            //eventt.EndingTime = eventDataModelTemp.EndingTime;
            //eventt.AlertStatus = eventDataModelTemp.AlertStatus;
            //UserName = username;
            return View(userr);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult editAccount(string username, User userr)
        {
            //if (UserName == "")
            //{
            //    return View();
            //}
            ViewData["DuplicateEventTitle"] = false;
            if (!ModelState.IsValid)
            {
                return View();
            }

            List<UserDataModel> userList = UserDataAccess.GetUsers();
            foreach (UserDataModel userDataModelTemp in userList)
            {
                if (userDataModelTemp.Username == userr.Username && userDataModelTemp.Username!=username)//UserName)
                {
                    ViewData["DuplicateUsername"] = true;
                    ViewData["User"] = username;
                    //Prepei edo na valo UserName=""; ?
                    return View();
                }
                //if (userDataModelTemp.Password == userr.Password && userDataModelTemp.Username !=username)// UserName)
                //{
                //    ViewData["DuplicateEventTitle"] = true;
                //    ViewData["User"] = username;
                //    return View();
                //}
                //if (userDataModelTemp.Fullname == userr.Fullname && userDataModelTemp.Username != username)// UserName)
                //{
                //    ViewData["DuplicateEventTitle"] = true;
                //    ViewData["User"] = username;
                //    return View();
                //}
                if (userDataModelTemp.Email == userr.Email && userDataModelTemp.Username != username)// UserName)
                {
                    ViewData["DuplicateEmail"] = true;
                    ViewData["User"] = username;
                    return View();
                }
                //if (userDataModelTemp.Phone == userr.Phone && userDataModelTemp.Username != username)// UserName)
                //{
                //    ViewData["DuplicateEventTitle"] = true;
                //    ViewData["User"] = username;
                //    return View();
                //}

            }
            UserDataModel userDataModel = new UserDataModel();
            userDataModel.Username = userr.Username;
            userDataModel.Password = userr.Password;
            userDataModel.Phone = userr.Phone;
            userDataModel.Email = userr.Email;
            userDataModel.Fullname = userr.Fullname;
            UserDataAccess.UpdateUser(userDataModel);
            //UserDataAccess.UpdateUserAndUsername(userDataModel, username);// UserName);
            return RedirectToAction("HomePage", "Home", new { username = username, pagination = 1 });
        }
    }
}
