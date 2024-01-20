﻿namespace Savi_Thrift.Common.Utilities
{
    public class PageResult<T>
    {
        public IEnumerable<T> Data { get; set; }
        public int TotalPageCount { get; set; }
        public int CurrentPage { get; set; }
        public int PerPage { get; set; }
        public int TotalCount { get; set; }

    }
}
