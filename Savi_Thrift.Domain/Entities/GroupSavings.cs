﻿using Microsoft.AspNetCore.Http;
using Savi_Thrift.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Savi_Thrift.Domain.Entities
{
    public class GroupSavings : BaseEntity
    {
        public string UserId { get; set; }
        public string GroupName { get; set; }
        public decimal ContributionAmount { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public DateTime ActualStartDate { get; set; }
        public DateTime ExpectedEndDate { get; set;}
        public SavingFrequency Frequency { get; set;}
        public int MemberCount { get; set; }
        public DateTime RunTime { get; set; }
        public string PurposeAndGoal { get; set;}
        public string TermsAndConditions { get; set;}
        public GroupStatus GroupStatus { get; set;}
        public string SafePortraitImageURL  { get; set;}
        public string SafeLandScapeImageURL { get; set; }
        public DateTime NextRunTime { get; set; }
        public bool IsOpen { get; set; }

    }
}