using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPOAzBlob.Engine
{
    /// <summary>
    /// Base for all logical exceptions
    /// </summary>
    public abstract class SPOAzBlobException: Exception
    {
    }

    /// <summary>
    /// Exception during a file update
    /// </summary>
    public abstract class UpdateConflictException : SPOAzBlobException
    {
    }

    public abstract class UpdateConflictAgainstOtherUserException : UpdateConflictException
    {
        public UpdateConflictAgainstOtherUserException(string otherUser) :base()
        { 
            OtherUser = otherUser;
        }

        public string OtherUser { get; set; } = string.Empty;
    }

    /// <summary>
    /// Trying to lock a file that's already locked by someone else
    /// </summary>
    public class SetLockFileLockedByAnotherUserException : UpdateConflictAgainstOtherUserException
    {
        public SetLockFileLockedByAnotherUserException(string otherUser) : base(otherUser)
        {
        }
    }

    /// <summary>
    /// Trying to update a file that's been changed since lock
    /// </summary>
    public class SetLockFileUpdateConflictException : UpdateConflictException
    {
    }

    /// <summary>
    /// File in SPO already has a lock
    /// </summary>
    public class SpoFileAlreadyBeingEditedException : UpdateConflictException
    {
    }
}
