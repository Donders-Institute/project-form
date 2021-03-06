﻿using System;
using Dccn.ProjectForm.Models;

namespace Dccn.ProjectForm.Extensions
{
    public static class ApprovalStatusExtensions
    {
        public static string GetColor(this ApprovalStatusModel status)
        {
            switch (status)
            {
                case ApprovalStatusModel.NotSubmitted:
                    return "secondary";
                case ApprovalStatusModel.NotApplicable:
                    return "dark";
                case ApprovalStatusModel.Pending:
                    return "info";
                case ApprovalStatusModel.Approved:
                    return "success";
                case ApprovalStatusModel.Rejected:
                    return "danger";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static int GetRanking(this ApprovalStatusModel status)
        {
            switch (status)
            {
                case ApprovalStatusModel.Rejected:
                    return 0;
                case ApprovalStatusModel.Pending:
                    return 1;
                case ApprovalStatusModel.Approved:
                    return 2;
                case ApprovalStatusModel.NotSubmitted:
                    return 3;
                case ApprovalStatusModel.NotApplicable:
                    return 4;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static string GetIconClasses(this ApprovalStatusModel status)
        {
            switch (status)
            {
                case ApprovalStatusModel.NotSubmitted:
                    return null;
                case ApprovalStatusModel.Pending:
                    return "fas fa-ellipsis-h";
                case ApprovalStatusModel.Approved:
                    return "fas fa-check";
                case ApprovalStatusModel.Rejected:
                    return "fas fa-times";
                case ApprovalStatusModel.NotApplicable:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}