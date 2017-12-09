using System;

namespace KdSoft.Lmdb
{
    /// <summary>
    /// Unix file access privilegies
    /// </summary>
    [Flags]
    public enum UnixFileMode : uint
    {
        /// <summary>
        /// S_IRUSR, read permission, owner
        /// </summary>
        OwnerRead = 0x0100,

        /// <summary>
        /// S_IWUSR, write permission, owner
        /// </summary>
        OwnerWrite = 0x0080,

        /// <summary>
        /// S_IXUSR, execute/search permission, owner
        /// </summary>
        OwnerExec = 0x0040,

        /// <summary>
        /// S_IRGRP, read permission, group
        /// </summary>
        GroupRead = 0x0020,

        /// <summary>
        /// S_IWGRP, write permission, group
        /// </summary>
        GroupWrite = 0x0010,

        /// <summary>
        /// S_IXGRP, execute/search permission, group
        /// </summary>
        GroupExec = 0x0008,

        /// <summary>
        /// S_IROTH, read permission, others
        /// </summary>
        OtherRead = 0x0004,

        /// <summary>
        /// S_IWOTH, write permission, others
        /// </summary>
        OtherWrite = 0x0002,

        /// <summary>
        /// S_IXOTH, execute/search permission, others
        /// </summary>
        OtherExec = 0x0001,

        /// <summary>
        /// Owner, Group, Other Read/Write
        /// </summary>
        Default = OwnerRead | OwnerWrite | GroupRead | GroupWrite | OtherRead | OtherWrite,

        /// <summary>
        /// Owner all permissions
        /// </summary>
        OwnerAll = OwnerRead | OwnerWrite | OwnerExec,

        /// <summary>
        /// Group all permissions
        /// </summary>
        GroupAll = GroupRead | GroupWrite | GroupExec,

        /// <summary>
        /// Others all permissions
        /// </summary>
        OtherAll = OtherRead | OtherWrite | OtherExec,
    }
}
