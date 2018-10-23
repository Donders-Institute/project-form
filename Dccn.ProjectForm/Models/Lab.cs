﻿using System.ComponentModel;

namespace Dccn.ProjectForm.Models
{
    public class Lab
    {
        public int? Id { get; set; }

        public Modality Modality { get; set; }

        [DisplayName("Subjects")]
        public int? SubjectCount { get; set; }

        [DisplayName("Extra subjects")]
        public int? ExtraSubjectCount { get; set; }

        [DisplayName("Sessions")]
        public int? SessionCount { get; set; }

        [DisplayName("Duration")]
        public int? SessionDurationMinutes { get; set; }
    }
}