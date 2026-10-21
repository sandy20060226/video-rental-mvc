using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class OrderController : Controller
    {
        private readonly string _connectionString;

        public OrderController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("尚未設定資料庫連線。");
        }

        public IActionResult Index(string searchKeyword, int page = 1)
        {
            int pageSize = 5;
            int totalRecords = 0;
            List<Order> videos = new List<Order>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string countSql = @"SELECT COUNT(*) FROM TableVideos_1131619 
                                    WHERE (@Search IS NULL 
                                           OR [title] LIKE '%' + @Search + '%' 
                                           OR [type] LIKE '%' + @Search + '%'
                                           OR [director] LIKE '%' + @Search + '%')";

                using (SqlCommand cmd = new SqlCommand(countSql, conn))
                {
                    cmd.Parameters.AddWithValue("@Search", (object)searchKeyword ?? DBNull.Value);
                    totalRecords = (int)cmd.ExecuteScalar();
                }

                // 🌟 連接 TableAgency 撈出代理商資料
                string sql = @"SELECT v.[vNo], v.[title], v.[type], v.[grade], v.[director]
                               FROM TableVideos_1131619 v
                               ORDER BY v.[vNo] DESC 
                               OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Search", (object)searchKeyword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            videos.Add(new Order
                            {
                                vNo = Convert.ToInt32(reader["vNo"]),
                                title = reader["title"].ToString(),
                                type = reader["type"].ToString(),
                                grade = reader["grade"].ToString(),
                                director = reader["director"].ToString(),
                                VideoCopies = new List<VideoCopyDto>()
                            });
                        }
                    }
                }

                // 撈取每一部影片的拷貝，並實時封裝代理商與會員詳情
                foreach (var video in videos)
                {
                    // 這裡我們暫時模擬 Agency 資料，如果資料庫之後有蓋 TableAgency_1131619，可以改成 LEFT JOIN
                    string copySql = @"SELECT c.[cNo], c.[media], c.[date], c.[mId]
                                       FROM TableVideoCopies_1131619 c
                                       WHERE c.[vNo] = @vNo";

                    using (SqlCommand cmd = new SqlCommand(copySql, conn))
                    {
                        cmd.Parameters.AddWithValue("@vNo", video.vNo);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var copy = new VideoCopyDto
                                {
                                    cNo = Convert.ToInt32(reader["cNo"]),
                                    Media = reader["media"].ToString(),
                                    Date = Convert.ToDateTime(reader["date"]),
                                    MId = reader["mId"] == DBNull.Value ? null : reader["mId"].ToString(),

                                    // 🌟 寫入預設的代理商基本資訊 (符合 ER 圖欄位)
                                    UCode = "88371029",
                                    AName = "巨星影視發行代理公司",
                                    Contact = "陳大明經理",
                                    AgencyPhone = "02-2345-6789",
                                    AgencyAddress = "台北市中山區南京東路二段100號"
                                };

                                // 如果該拷貝已被會員借出，撈出該會員完整明細與他目前租借的所有種類
                                if (!string.IsNullOrEmpty(copy.MId))
                                {
                                    copy.MemberInfo = GetMemberDetails(copy.MId, conn);
                                }

                                video.VideoCopies.Add(copy);
                            }
                        }
                    }
                }
            }

            ViewBag.SearchKeyword = searchKeyword;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            if (ViewBag.TotalPages < 1) ViewBag.TotalPages = 1;

            return View(videos);
        }

        // 🌟 輔助方法：實時取得會員基本資料及租借種類清單
        // 🌟 修改後的輔助方法：實時取得會員基本資料及租借影片的「種類 + 片名」
        private MemberDto GetMemberDetails(string mId, SqlConnection conn)
        {
            MemberDto member = null;
            string memberSql = "SELECT [mId], [name], [phone], [startDate], [credits], [address] FROM TableMembers_1131619 WHERE [mId] = @mId";

            using (SqlCommand cmd = new SqlCommand(memberSql, conn))
            {
                cmd.Parameters.AddWithValue("@mId", mId);
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        member = new MemberDto
                        {
                            MId = reader["mId"].ToString(),
                            Name = reader["name"].ToString(),
                            Phone = reader["phone"].ToString(),
                            StartDate = reader["startDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["startDate"]),
                            Credits = reader["credits"] == DBNull.Value ? 50 : Convert.ToInt32(reader["credits"]),
                            Address = reader["address"].ToString()
                        };
                    }
                }
            }

            if (member != null)
            {
                // 🌟 核心修改：使用 JOIN 撈出該會員目前借出的 拷貝種類 (media) + 影片名稱 (title)
                string rentListSql = @"SELECT c.[media], v.[title] 
                               FROM TableVideoCopies_1131619 c
                               JOIN TableVideos_1131619 v ON c.[vNo] = v.[vNo]
                               WHERE c.[mId] = @mId";

                using (SqlCommand cmd = new SqlCommand(rentListSql, conn))
                {
                    cmd.Parameters.AddWithValue("@mId", mId);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // 將格式組合成：「📀 種類 - 影片名稱」
                            string combinedInfo = $"📀 {reader["media"]} - {reader["title"]}";
                            member.RentedMedias.Add(combinedInfo);
                        }
                    }
                }
            }
            return member;
        }

        public IActionResult Rent(int id)
        {
            VideoCopyDto copy = null;
            List<SelectListItem> members = new List<SelectListItem>();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                string sql = @"SELECT c.[cNo], c.[vNo], c.[media], v.[title], c.[mId]
                               FROM TableVideoCopies_1131619 c
                               JOIN TableVideos_1131619 v ON c.[vNo] = v.[vNo]
                               WHERE c.[cNo] = @cNo";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@cNo", id);
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            copy = new VideoCopyDto
                            {
                                cNo = Convert.ToInt32(reader["cNo"]),
                                Media = reader["media"].ToString(),
                                VideoTitle = reader["title"].ToString(),
                                MId = reader["mId"] == DBNull.Value ? null : reader["mId"].ToString()
                            };
                        }
                    }
                }

                if (copy == null || copy.MId != null) return BadRequest("該拷貝不存在或已被借出。");

                string memberSql = "SELECT [mId], [name] FROM TableMembers_1131619";
                using (SqlCommand cmd = new SqlCommand(memberSql, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            members.Add(new SelectListItem
                            {
                                Value = reader["mId"].ToString(),
                                Text = $"{reader["mId"]} - {reader["name"]}"
                            });
                        }
                    }
                }
            }

            ViewBag.Members = members;
            return View(copy);
        }

        [HttpPost]
        public IActionResult RentConfirm(int cNo, string mId)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string sql = "UPDATE TableVideoCopies_1131619 SET [mId] = @mId WHERE [cNo] = @cNo AND [mId] IS NULL";
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@mId", mId);
                    cmd.Parameters.AddWithValue("@cNo", cNo);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index", "Order");
        }
    }
}