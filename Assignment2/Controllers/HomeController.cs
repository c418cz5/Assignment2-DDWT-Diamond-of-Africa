using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace Assignment2.Controllers
{
	public class HomeController : Controller
	{
		// Extend VideoModel: Add fields required for Details page (cast, release country, rating)
		public class VideoModel
		{
			public string videoId { get; set; }
			public string title { get; set; }
			public string releaseYear { get; set; }
			public string genres { get; set; }
			public string directorName { get; set; }
			public string castNames { get; set; }
			public string releaseCountry { get; set; }
			public string rating { get; set; }
		}

		// Index Page: Display show list + search by title
		public ActionResult Index(string searchTitle = "")
		{
			string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShowDB"].ConnectionString;
			List<VideoModel> showList = new List<VideoModel>();

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
							showList.Add(new VideoModel
							{
								videoId = reader["videoId"].ToString(),
								title = reader["title"].ToString(),
								releaseYear = reader["releaseYear"]?.ToString() ?? "",
								genres = reader["genres"]?.ToString() ?? "N/A",
								directorName = reader["directorName"]?.ToString() ?? "N/A",
								castNames = "",
								releaseCountry = "",
								rating = ""
							});
						}
					}
				}
			}

			ViewBag.Shows = showList;
			ViewBag.SearchKey = searchTitle;

			return View();
		}

		// Details Page: Get single show details by videoId
		public ActionResult Details(string videoId)
		{
			// Validate videoId (prevent invalid access)
			if (string.IsNullOrEmpty(videoId))
			{
				return HttpNotFound("Show ID does not exist");
			}

			string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShowDB"].ConnectionString;
			VideoModel showDetails = null;

			using (SqlConnection connection = new SqlConnection(connStr))
			{
				string query = @"SELECT 
                                      v.videoId,
                                      v.videoTitle AS title,
                                      v.releaseYear,
                                      STRING_AGG(DISTINCT g.genre, ', ') AS genres,
                                      STRING_AGG(DISTINCT d.directorName, ', ') AS directorName,
                                      STRING_AGG(DISTINCT c.castName, ', ') AS castNames,
                                      v.releaseCountry,
                                      v.rating
                               FROM videos v
                               LEFT JOIN videoGenres vg ON v.videoId = vg.videoId
                               LEFT JOIN Genres g ON vg.genreId = g.genreId
                               LEFT JOIN VideoDirectors vd ON v.videoId = vd.videoId
                               LEFT JOIN directors d ON vd.directorId = d.directorId
                               LEFT JOIN VideoCasts vc ON v.videoId = vc.videoId
                               LEFT JOIN Casts c ON vc.castId = c.castId
                               WHERE v.videoId = @VideoId
                               GROUP BY v.videoId, v.videoTitle, v.releaseYear, v.releaseCountry, v.rating";

				using (SqlCommand command = new SqlCommand(query, connection))
				{
					command.CommandTimeout = 60;
					command.Parameters.AddWithValue("@VideoId", videoId);

					connection.Open();
					using (SqlDataReader reader = command.ExecuteReader())
					{
						if (reader.Read())
						{
							showDetails = new VideoModel
							{
								videoId = reader["videoId"].ToString(),
								title = reader["title"].ToString(),
								releaseYear = reader["releaseYear"]?.ToString() ?? "N/A",
								genres = reader["genres"]?.ToString() ?? "N/A",
								directorName = reader["directorName"]?.ToString() ?? "N/A",
								castNames = reader["castNames"]?.ToString() ?? "N/A",
								releaseCountry = reader["releaseCountry"]?.ToString() ?? "N/A",
								rating = reader["rating"]?.ToString() ?? "Not Rated"
							};
						}
					}
				}
			}

			// Validate if show exists
			if (showDetails == null)
			{
				return HttpNotFound("Show details not found");
			}

			ViewBag.Show = showDetails;
			return View();
		}
	}
}