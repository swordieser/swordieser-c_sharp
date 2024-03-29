﻿using System;
using System.Collections.Generic;
using Isu.Tools;

namespace Isu.Services
{
    public class CourseNumber
    {
        private int _courseNumber;

        public CourseNumber(int number)
        {
            if (number > 4 || number < 1)
            {
                throw new InvalidCourseNumber();
            }

            _courseNumber = number;
            Groups = new List<Group>();
        }

        public List<Group> Groups { get; }

        public static int StringToIntNumber(string name)
        {
            if (name.Length != 5)
            {
                throw new InvalidGroupNameException();
            }

            try
            {
                int temp = int.Parse(name.Substring(2, 1));
            }
            catch (Exception)
            {
                throw new InvalidGroupNameException();
            }

            return int.Parse(name.Substring(2, 1)) - 1;
        }
    }
}