using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace Assignment2.Controllers
{
	public class HomeController : Controller
	{
		public class DirectorModel
		{
			public string directorId { get; set; }
			public string directorName { get; set; }
		}

		public class CastModel
		{
			public string castId { get; set; }
			public string castName { get; set; }
		}

		public class VideoModel
		{
			public string videoId { get; set; }
			public string title { get; set; }
			public string releaseYear { get; set; }
			public string genres { get; set; }
			public string directorName { get; set; }
			public string castNames { get; set; }
		}

		public class GenreModel
		{
			public string genreId { get; set; }
			public string genre { get; set; }
		}

		public ActionResult Index(string searchTitle = "", string searchGenre = "")
		{
			string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShowDB"].ConnectionString;
			List<VideoModel> showList = new List<VideoModel>();
			List<GenreModel> genreList = new List<GenreModel>();

			using (SqlConnection connGenre = new SqlConnection(connStr))
			{
				string genreQuery = "SELECT genreId, genre FROM Genres ORDER BY genre";
				using (SqlCommand cmdGenre = new SqlCommand(genreQuery, connGenre))
				{
					connGenre.Open();
					using (SqlDataReader reader = cmdGenre.ExecuteReader())
					{
						while (reader.Read())
						{
							genreList.Add(new GenreModel
							{
								genreId = reader["genreId"].ToString(),
								genre = reader["genre"].ToString()
							});
						}
					}
				}
			}
			ViewBag.Genres = genreList;
			ViewBag.SearchGenre = searchGenre;
			ViewBag.SearchKey = searchTitle;

			using (SqlConnection connection = new SqlConnection(connStr))
			{
				string query = @"SELECT v.videoId, v.videoTitle AS title,
                                      v.releaseYear,
                                      (SELECT STRING_AGG(g.genre, ', ') 
                                       FROM (SELECT DISTINCT g.genre 
                                             FROM videoGenres vg 
                                             LEFT JOIN Genres g ON vg.genreId = g.genreId 
                                             WHERE vg.videoId = v.videoId) g) AS genres,
                                      (SELECT STRING_AGG(d.directorName, ', ') 
                                       FROM (SELECT DISTINCT d.directorName 
                                             FROM VideoDirectors vd 
                                             LEFT JOIN directors d ON vd.directorId = d.directorId 
                                             WHERE vd.videoId = v.videoId) d) AS directorName
                               FROM videos v";

				List<string> whereConditions = new List<string>();
				if (!string.IsNullOrEmpty(searchTitle))
				{
					whereConditions.Add("v.videoTitle LIKE @SearchTerm");
				}
				if (!string.IsNullOrEmpty(searchGenre))
				{
					whereConditions.Add("EXISTS (SELECT 1 FROM videoGenres vg WHERE vg.videoId = v.videoId AND vg.genreId = @GenreId)");
				}

				if (whereConditions.Count > 0)
				{
					query += " WHERE " + string.Join(" AND ", whereConditions);
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
					if (!string.IsNullOrEmpty(searchGenre))
					{
						command.Parameters.AddWithValue("@GenreId", searchGenre);
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
								releaseYear = reader["releaseYear"]?.ToString() ?? "N/A",
								genres = reader["genres"]?.ToString() ?? "N/A",
								directorName = reader["directorName"]?.ToString() ?? "N/A",
								castNames = ""
							});
						}
					}
				}
			}

			ViewBag.Shows = showList;
			return View();
		}


		public ActionResult Details(string id)
		{
			if (string.IsNullOrEmpty(id))
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
                                      (SELECT STRING_AGG(g.genre, ', ') 
                                       FROM (SELECT DISTINCT g.genre 
                                             FROM videoGenres vg 
                                             LEFT JOIN Genres g ON vg.genreId = g.genreId 
                                             WHERE vg.videoId = v.videoId) g) AS genres,
                                      (SELECT STRING_AGG(d.directorName, ', ') 
                                       FROM (SELECT DISTINCT d.directorName 
                                             FROM VideoDirectors vd 
                                             LEFT JOIN directors d ON vd.directorId = d.directorId 
                                             WHERE vd.videoId = v.videoId) d) AS directorName,
                                      (SELECT STRING_AGG(c.castName, ', ') 
                                       FROM (SELECT DISTINCT c.castName 
                                             FROM VideoCasts vc 
                                             LEFT JOIN Casts c ON vc.castId = c.castId 
                                             WHERE vc.videoId = v.videoId) c) AS castNames
                               FROM videos v
                               WHERE v.videoId = @VideoId
                               GROUP BY v.videoId, v.videoTitle, v.releaseYear";

				using (SqlCommand command = new SqlCommand(query, connection))
				{
					command.CommandTimeout = 60;
					command.Parameters.AddWithValue("@VideoId", id); 

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
								directorName = reader["directorName"].ToString() ?? "N/A",
								castNames = reader["castNames"]?.ToString() ?? "N/A"
							};
						}
					}
				}
			}

			if (showDetails == null)
			{
				return HttpNotFound("Show details not found");
			}

			ViewBag.Show = showDetails;
			return View();
		}

		public ActionResult AddShow()
		{
			string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShowDB"].ConnectionString;
			List<GenreModel> genreList = new List<GenreModel>();
			List<DirectorModel> directorList = new List<DirectorModel>();
			List<CastModel> castList = new List<CastModel>();

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string genreQuery = "SELECT genreId, genre FROM Genres ORDER BY genre";
				using (SqlCommand cmd = new SqlCommand(genreQuery, conn))
				{
					conn.Open();
					SqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						genreList.Add(new GenreModel { genreId = reader["genreId"].ToString(), genre = reader["genre"].ToString() });
					}
				}
			}

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string dirQuery = "SELECT directorId, directorName FROM Directors ORDER BY directorName";
				using (SqlCommand cmd = new SqlCommand(dirQuery, conn))
				{
					conn.Open();
					SqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						directorList.Add(new DirectorModel
						{
							directorId = reader["directorId"].ToString(),
							directorName = reader["directorName"].ToString()
						});
					}
				}
			}

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string castQuery = "SELECT castId, castName FROM Casts ORDER BY castName";
				using (SqlCommand cmd = new SqlCommand(castQuery, conn))
				{
					conn.Open();
					SqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						castList.Add(new CastModel { castId = reader["castId"].ToString(), castName = reader["castName"].ToString() });
					}
				}
			}

			ViewBag.Genres = genreList;
			ViewBag.Directors = directorList;
			ViewBag.Casts = castList;
			return View();
		}

		[HttpPost]
		public ActionResult AddShow(string title, string releaseYear, string rating, string[] genreIds, string existingDirectorId, string newDirectorName, string[] castIds)
		{
			string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShowDB"].ConnectionString;
			string newVideoId = Guid.NewGuid().ToString().Substring(0, 8);

			if (string.IsNullOrEmpty(title) || genreIds == null || genreIds.Length == 0 || castIds == null || castIds.Length == 0)
			{
				TempData["Error"] = "Title, Genre and Cast are required!";
				return RedirectToAction("AddShow");
			}

			string directorId = existingDirectorId;
			if (!string.IsNullOrEmpty(newDirectorName) && string.IsNullOrEmpty(existingDirectorId))
			{
				using (SqlConnection conn = new SqlConnection(connStr))
				{
					string newDirId = Guid.NewGuid().ToString().Substring(0, 8);
					string dirQuery = "INSERT INTO Directors (directorId, directorName) VALUES (@dirId, @dirName)";
					using (SqlCommand cmd = new SqlCommand(dirQuery, conn))
					{
						cmd.Parameters.AddWithValue("@dirId", newDirId);
						cmd.Parameters.AddWithValue("@dirName", newDirectorName.Trim());
						conn.Open();
						cmd.ExecuteNonQuery();
						directorId = newDirId;
					}
				}
			}

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string videoQuery = "INSERT INTO Videos (videoId, videoTitle, releaseYear, rating) VALUES (@vidId, @title, @year, @rating)";
				using (SqlCommand cmd = new SqlCommand(videoQuery, conn))
				{
					cmd.Parameters.AddWithValue("@vidId", newVideoId);
					cmd.Parameters.AddWithValue("@title", title.Trim());
					cmd.Parameters.AddWithValue("@year", string.IsNullOrEmpty(releaseYear) ? DBNull.Value : (object)releaseYear);
					cmd.Parameters.AddWithValue("@rating", string.IsNullOrEmpty(rating) ? DBNull.Value : (object)rating);
					conn.Open();
					cmd.ExecuteNonQuery();

					foreach (var genreId in genreIds)
					{
						string genreQuery = "INSERT INTO VideoGenres (videoId, genreId) VALUES (@vidId, @genreId)";
						using (SqlCommand cmdGenre = new SqlCommand(genreQuery, conn))
						{
							cmdGenre.Parameters.AddWithValue("@vidId", newVideoId);
							cmdGenre.Parameters.AddWithValue("@genreId", genreId);
							cmdGenre.ExecuteNonQuery();
						}
					}

					if (!string.IsNullOrEmpty(directorId))
					{
						string dirQuery = "INSERT INTO VideoDirectors (videoId, directorId) VALUES (@vidId, @dirId)";
						using (SqlCommand cmdDir = new SqlCommand(dirQuery, conn))
						{
							cmdDir.Parameters.AddWithValue("@vidId", newVideoId);
							cmdDir.Parameters.AddWithValue("@dirId", directorId);
							cmdDir.ExecuteNonQuery();
						}
					}

					foreach (var castId in castIds)
					{
						string castQuery = "INSERT INTO VideoCasts (videoId, castId) VALUES (@vidId, @castId)";
						using (SqlCommand cmdCast = new SqlCommand(castQuery, conn))
						{
							cmdCast.Parameters.AddWithValue("@vidId", newVideoId);
							cmdCast.Parameters.AddWithValue("@castId", castId);
							cmdCast.ExecuteNonQuery();
						}
					}
				}
			}

			TempData["Success"] = "Show added successfully!";
			return RedirectToAction("Index");
		}

		public ActionResult Update(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return HttpNotFound("Show ID does not exist");
			}

			string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShowDB"].ConnectionString;
			VideoModel show = null;
			List<GenreModel> genreList = new List<GenreModel>();
			List<DirectorModel> directorList = new List<DirectorModel>();
			List<CastModel> castList = new List<CastModel>();

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string query = @"SELECT v.videoId, v.videoTitle AS title, v.releaseYear, v.rating,
                                      (SELECT STRING_AGG(vg.genreId, ',') FROM videoGenres vg WHERE vg.videoId = v.videoId) AS genreIds,
                                      (SELECT TOP 1 vd.directorId FROM VideoDirectors vd WHERE vd.videoId = v.videoId) AS directorId,
                                      (SELECT STRING_AGG(vc.castId, ',') FROM VideoCasts vc WHERE vc.videoId = v.videoId) AS castIds
                               FROM videos v
                               WHERE v.videoId = @VideoId";

				using (SqlCommand cmd = new SqlCommand(query, conn))
				{
					cmd.Parameters.AddWithValue("@VideoId", id);
					conn.Open();
					using (SqlDataReader reader = cmd.ExecuteReader())
					{
						if (reader.Read())
						{
							show = new VideoModel
							{
								videoId = reader["videoId"].ToString(),
								title = reader["title"].ToString(),
								releaseYear = reader["releaseYear"]?.ToString() ?? "",
								genres = reader["genreIds"]?.ToString() ?? ""
							};
							ViewBag.CurrentRating = reader["rating"]?.ToString() ?? "";
							ViewBag.CurrentDirectorId = reader["directorId"]?.ToString() ?? "";
							ViewBag.CurrentCastIds = reader["castIds"]?.ToString()?.Split(',') ?? new string[0];
						}
					}
				}
			}

			if (show == null)
			{
				return HttpNotFound("Show details not found");
			}

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string genreQuery = "SELECT genreId, genre FROM Genres ORDER BY genre";
				using (SqlCommand cmd = new SqlCommand(genreQuery, conn))
				{
					conn.Open();
					SqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						genreList.Add(new GenreModel { genreId = reader["genreId"].ToString(), genre = reader["genre"].ToString() });
					}
				}
			}

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string dirQuery = "SELECT directorId, directorName FROM Directors ORDER BY directorName";
				using (SqlCommand cmd = new SqlCommand(dirQuery, conn))
				{
					conn.Open();
					SqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						directorList.Add(new DirectorModel { directorId = reader["directorId"].ToString(), directorName = reader["directorName"].ToString() });
					}
				}
			}

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string castQuery = "SELECT castId, castName FROM Casts ORDER BY castName";
				using (SqlCommand cmd = new SqlCommand(castQuery, conn))
				{
					conn.Open();
					SqlDataReader reader = cmd.ExecuteReader();
					while (reader.Read())
					{
						castList.Add(new CastModel { castId = reader["castId"].ToString(), castName = reader["castName"].ToString() });
					}
				}
			}

			ViewBag.Show = show;
			ViewBag.Genres = genreList;
			ViewBag.Directors = directorList;
			ViewBag.Casts = castList;
			return View();
		}

		[HttpPost]
		public ActionResult Update(string id, string title, string releaseYear, string rating, string[] genreIds, string existingDirectorId, string[] castIds)
		{
			if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(title) || genreIds == null || genreIds.Length == 0 || castIds == null || castIds.Length == 0)
			{
				TempData["Error"] = "Title, Genre and Cast are required!";
				return RedirectToAction("Update", new { id = id });
			}

			string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["ShowDB"].ConnectionString;

			using (SqlConnection conn = new SqlConnection(connStr))
			{
				string videoQuery = "UPDATE Videos SET videoTitle = @title, releaseYear = @year, rating = @rating WHERE videoId = @videoId";
				using (SqlCommand cmd = new SqlCommand(videoQuery, conn))
				{
					cmd.Parameters.AddWithValue("@videoId", id);
					cmd.Parameters.AddWithValue("@title", title.Trim());
					cmd.Parameters.AddWithValue("@year", string.IsNullOrEmpty(releaseYear) ? DBNull.Value : (object)releaseYear);
					cmd.Parameters.AddWithValue("@rating", string.IsNullOrEmpty(rating) ? DBNull.Value : (object)rating);
					conn.Open();
					cmd.ExecuteNonQuery();

					string delGenre = "DELETE FROM VideoGenres WHERE videoId = @videoId";
					using (SqlCommand cmdDel = new SqlCommand(delGenre, conn))
					{
						cmdDel.Parameters.AddWithValue("@videoId", id);
						cmdDel.ExecuteNonQuery();
					}
					foreach (var genreId in genreIds)
					{
						string addGenre = "INSERT INTO VideoGenres (videoId, genreId) VALUES (@videoId, @genreId)";
						using (SqlCommand cmdAdd = new SqlCommand(addGenre, conn))
						{
							cmdAdd.Parameters.AddWithValue("@videoId", id);
							cmdAdd.Parameters.AddWithValue("@genreId", genreId);
							cmdAdd.ExecuteNonQuery();
						}
					}

					string delDir = "DELETE FROM VideoDirectors WHERE videoId = @videoId";
					using (SqlCommand cmdDel = new SqlCommand(delDir, conn))
					{
						cmdDel.Parameters.AddWithValue("@videoId", id);
						cmdDel.ExecuteNonQuery();
					}
					if (!string.IsNullOrEmpty(existingDirectorId))
					{
						string addDir = "INSERT INTO VideoDirectors (videoId, directorId) VALUES (@videoId, @dirId)";
						using (SqlCommand cmdAdd = new SqlCommand(addDir, conn))
						{
							cmdAdd.Parameters.AddWithValue("@videoId", id);
							cmdAdd.Parameters.AddWithValue("@dirId", existingDirectorId);
							cmdAdd.ExecuteNonQuery();
						}
					}

					string delCast = "DELETE FROM VideoCasts WHERE videoId = @videoId";
					using (SqlCommand cmdDel = new SqlCommand(delCast, conn))
					{
						cmdDel.Parameters.AddWithValue("@videoId", id);
						cmdDel.ExecuteNonQuery();
					}
					foreach (var castId in castIds)
					{
						string addCast = "INSERT INTO VideoCasts (videoId, castId) VALUES (@videoId, @castId)";
						using (SqlCommand cmdAdd = new SqlCommand(addCast, conn))
						{
							cmdAdd.Parameters.AddWithValue("@videoId", id);
							cmdAdd.Parameters.AddWithValue("@castId", castId);
							cmdAdd.ExecuteNonQuery();
						}
					}
				}
			}

			TempData["Success"] = "Show updated successfully!";
			return RedirectToAction("Details", new { id = id });
		}
	}
}