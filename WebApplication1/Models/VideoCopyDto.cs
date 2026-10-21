using System;
using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class VideoCopyDto
    {
        public int cNo { get; set; }
        public int VNo { get; set; }
        public string Media { get; set; }
        public DateTime Date { get; set; } // 拷貝日期
        public string MId { get; set; }

        public string VideoTitle { get; set; }

        // ====== 🌟 新增：代理商詳細資訊 ======
        public string UCode { get; set; }      // 統一編號
        public string AName { get; set; }      // 代理商名稱
        public string Contact { get; set; }    // 負責人
        public string AgencyPhone { get; set; } // 電話
        public string AgencyAddress { get; set; } // 地址

        // ====== 🌟 新增：會員詳細資訊 ======
        public MemberDto MemberInfo { get; set; }
    }

    public class MemberDto
    {
        public string MId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public DateTime? StartDate { get; set; }
        public int Credits { get; set; }
        public string Address { get; set; }
        // 記錄該會員目前租借的所有影片拷貝種類
        public List<string> RentedMedias { get; set; } = new List<string>();
    }
}