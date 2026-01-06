using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace Assignment2.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index(string searchTitle = "")
		{
			string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShowDB"].ConnectionString;

			List<dynamic> showList = new List<dynamic>();

			using (SqlConnection connection = new SqlConnection(connStr))
			{
				string query = @"SELECT v.videoId, v.videoTitle AS title,
                                      v.releaseYear,
                                      STRING_AGG(g.genre, ', ') AS genres,
                                      STRING_AGG(d.directorName, ', ') AS directorName
                               FROM videos v
                               LEFT JOIN videoGenres vg ON v.videoId = vg.videoId
                               LEFT JOIN Genres g ON vg.genreId = g.genreId
                               LEFT JOIN VideoDirectors vd ON v.videoId = vd.videoId
                               LEFT JOIN directors d ON vd.directorId = d.directorId";

				if (!string.IsNullOrEmpty(searchTitle))
				{
					query += " WHERE v.videoTitle LIKE @SearchTerm";
				}

				query += @" GROUP BY v.videoId, v.videoTitle, v.releaseYear
                           ORDER BY v.videoTitle";

				using (SqlCommand command = new SqlCommand(query, connection))
				{
					command.CommandTimeout = 60;
					if (!string.IsNullOrEmpty(searchTitle))
					{
						command.Parameters.AddWithValue("@SearchTerm", "%" + searchTitle + "%");
					}

					connection.Open();
					using (SqlDataReader reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							showList.Add(new
							{
								videoId = reader["videoId"],
								title = reader["title"],
								releaseYear = reader["releaseYear"],
								genres = reader["genres"],
								directorName = reader["directorName"]
							});
						}
					}
				}
			}

			ViewBag.Shows = showList;
			ViewBag.SearchKey = searchTitle;

			wreturn View();
		}
	}
}  