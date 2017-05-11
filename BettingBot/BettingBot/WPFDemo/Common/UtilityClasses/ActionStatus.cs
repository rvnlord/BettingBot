using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFDemo.Common.UtilityClasses
{
    public class ActionStatus
    {
        public bool IsSuccess => ErrorCode == ErrorCode.Success;
        public bool IsFailure => !IsSuccess;
        public ErrorCode ErrorCode { get; set; }
        public string Message { get; set; }

        public ActionStatus(ErrorCode code, string message)
        {
            ErrorCode = code;
            Message = message;
        }

        public ActionStatus(string successMessage)
        {
            ErrorCode = ErrorCode.Success;
            Message = successMessage;
        }

        public static ActionStatus Success()
        {
            return new ActionStatus("Sukces");
        }

        public static bool operator !(ActionStatus actionStatus)
        {
            return actionStatus.IsFailure;
        }
    }

    public enum ErrorCode
    {
        Success = 0,
        CannotSetClipboardText
    }
}
