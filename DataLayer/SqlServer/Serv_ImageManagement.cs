using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient; // Changed to use SQL Server's data access library
using SchoolGrades.BusinessObjects;
using System.IO;
using System.Data.Common;
using System.Threading;
using System.Xml.Linq;

namespace SchoolGrades
{
    internal partial class SqlServer_DataLayer : DataLayer
    {

        internal override void CreateTableImage()
        {
            using (DbConnection conn = Connect())
            {
                //conn.Open();
                DbCommand cmd = conn.CreateCommand();
                string query;
                query = "CREATE TABLE Images(" +
                    "IdImage INT NOT NULL, " +
                    "imagePath VARCHAR (255), " +
                    "caption VARCHAR (45), " +
                    "PRIMARY KEY (IdImage)" +
                    ");";
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            };
        }

        internal override List<Image> GetAllImagesShownToAClassDuringLessons(Class Class, SchoolSubject Subject,
            DateTime DateStart = default(DateTime), DateTime DateFinish = default(DateTime))
        {
            List<Image> images = new List<Image>();
            using (SqlConnection conn = new SqlConnection()) // Changed to SqlConnection
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                string query;
                query = "SELECT * FROM Images" +
                        " JOIN Lessons_Images ON Images.idImage=Lessons_Images.idImage" +
                        " JOIN Lessons ON Lessons.idLesson=Lessons_Images.idLesson" +
                        " WHERE Lessons.idClass=@ClassId" +
                        " AND Lessons.idSchoolSubject=@SubjectId";
                if (DateStart != default(DateTime) && DateFinish != default(DateTime))
                    query += " AND Lessons.date BETWEEN @DateStart AND @DateFinish";
                cmd.CommandText = query;
                cmd.Parameters.AddWithValue("@ClassId", Class.IdClass);
                cmd.Parameters.AddWithValue("@SubjectId", Subject.IdSchoolSubject);
                cmd.Parameters.AddWithValue("@DateStart", DateStart);
                cmd.Parameters.AddWithValue("@DateFinish", DateFinish);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Image i = new Image();
                        i.IdImage = (int)reader["IdImage"];
                        i.Caption = (string)reader["Caption"];
                        i.RelativePathAndFilename = (string)reader["ImagePath"];
                        images.Add(i);
                    }
                }
            }
            return images;
        }
        //metodo adattato per sql per ottenere la caption delle immagini
        internal override List<string> GetCaptionsOfThisImage(string FileName)
        {
            List<string> captions = new List<string>();
            using (SqlConnection conn = new SqlConnection()) // Changed to SqlConnection
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                SqlDataReader reader;
                string query;
                query = "SELECT Caption FROM Images" +
                    " WHERE imagePath" + SqlLikeStatement(FileName) + ";";
                cmd.CommandText = query;
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    captions.Add((string)reader["CAPTION"]);
                }
                cmd.Dispose();
                reader.Dispose();
            }
            return captions;
        }

        internal override void EraseStudentsPhoto(int? IdStudent, string SchoolYear)
        {
            using (SqlConnection conn = new SqlConnection())
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                string query;
                query = "DELETE FROM StudentsPhoto_Students" +
                    " WHERE idStudents=" + IdStudent +
                    " AND idSchoolYear='" + SchoolYear + "'" +
                    ";";
                cmd.ExecuteNonQuery();
                cmd.Dispose();
            }
        }


        internal override string GetFilePhoto(int? IdStudent, string SchoolYear)
        {
            using (SqlConnection conn = new SqlConnection())
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                string query;
                query = "SELECT photoPath FROM StudentsPhoto_Students" +
                    " WHERE idStudents=" + IdStudent +
                    " AND idSchoolYear='" + SchoolYear + "'" +
                    ";";
                cmd.CommandText = query;
                string photoPath = (string)cmd.ExecuteScalar();
                cmd.Dispose();
                return photoPath;
            }
        }

        internal override void ChangeImagesPath(Class Class, DbCommand cmd)
        {
            using (SqlConnection conn = new SqlConnection())
            {
                conn.Open();
                SqlDataReader reader;
                cmd = conn.CreateCommand();
                string query;
                query = "SELECT TOP 1 Images.idImage, Images.imagePath" +
                " FROM Images" +
                " JOIN Lessons_Images ON Images.idImage=Lessons_Images.idImage" +
                " JOIN Lessons ON Lessons.idLesson = Lessons_Images.idLesson" +
                " WHERE Lessons.idClass=" + Class.IdClass +
                " ;";
                reader = (SqlDataReader)cmd.ExecuteReader();
                reader.Read();
                string originalPath = Path.GetDirectoryName(Safe.String(reader["imagePath"]));
                string originalFolder = originalPath.Substring(0, originalPath.IndexOf("\\"));
                reader.Close();
                string newFolder = Class.SchoolYear + "_" + Class.Abbreviation;

                // replace the folder name in Images path 
                query = "UPDATE Images" +
                    " SET imagePath=REPLACE(Images.imagePath,'" + originalFolder + "','" + newFolder + "')" +
                    " FROM Images Img" +
                    " JOIN Lessons_Images ON Img.IdImage=Lessons_Images.idImage" +
                    " JOIN Lessons ON Lessons.idLesson=Lessons_Images.idLesson" +
                    " WHERE Lessons.idClass=" + Class.IdClass +
                    ";";
                cmd.ExecuteNonQuery();
            }
        }

        internal override void SaveImagePath(int? id, string path)
        {
            using (SqlConnection conn = new SqlConnection())
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                string query;
                query = "UPDATE Images" +
                " SET imagePath=" + SqlString(path) + "" +
                " WHERE idImage=" + id +
                ";";
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            }
        }

        internal override int? SaveDemoStudentPhotoPath(string relativePath, DbCommand cmd)
        {
            int? nextId = null;
            try
            {
                cmd.CommandText = "SELECT MAX(idStudentsPhoto) FROM StudentsPhotos;";
                var firstColumn = cmd.ExecuteScalar();
                if (firstColumn != DBNull.Value)
                {
                    nextId = int.Parse(firstColumn.ToString()) + 1;
                }
                else
                {
                    nextId = 1;
                }
                cmd.CommandText = "INSERT INTO StudentsPhotos" +
                " (idStudentsPhoto, photoPath)" +
                " Values (" + SqlInt(nextId.ToString()) + "," + SqlString(relativePath) + ");";
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
            }
            return nextId;
        }

        internal override void RemoveImageFromLesson(Lesson Lesson, Image Image, bool AlsoEraseImageFile)
        {
            // delete from the link table
            using (SqlConnection conn = (SqlConnection)Connect())
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                string query;
                query = "DELETE FROM Lessons_Images" +
                    " WHERE idImage=" +
                    Image.IdImage +
                    ";";
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
                if (AlsoEraseImageFile)
                {
                    // delete from the Images table 
                    query = "DELETE FROM Images" +
                        " WHERE idImage=" +
                        Image.IdImage +
                        ";";
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                cmd.Dispose();
            }
        }

        internal override void SaveImage(Image Image)
        {
            using (SqlConnection conn = (SqlConnection)Connect())
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                string query;
                query = "UPDATE Images" +
                    " SET caption=" + SqlString(Image.Caption) + "" +
                    ", imagePath=" + SqlString(Image.RelativePathAndFilename) + "" +
                    " WHERE idImage=" +
                    Image.IdImage +
                    ";";
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
        }

        internal override Image FindImageWithGivenFile(string PathAndFileNameOfImage)
        {
            Image i = new Image();
            using (SqlConnection conn = (SqlConnection)Connect())
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                SqlDataReader reader;
                string query;
                query = "SELECT * FROM Images" +
                        " WHERE Images.imagePath=" +
                        SqlString(PathAndFileNameOfImage.Remove(0, Commons.PathImages.Length + 1)) +
                        ";";
                cmd.CommandText = query;
                reader = cmd.ExecuteReader();
                reader.Read(); // just one record ! 
                if (!reader.HasRows)
                    return null;
                i.IdImage = (int)reader["IdImage"];
                i.Caption = (string)reader["Caption"];
                i.RelativePathAndFilename = (string)reader["ImagePath"];
                cmd.Dispose();
                reader.Dispose();
            }
            return i;
        }

        internal override int? LinkOneImage(Image Image, Lesson Lesson)
        {
            using (SqlConnection conn = (SqlConnection)Connect())
            {
                conn.Open();
                SqlCommand cmd = conn.CreateCommand();
                string query;
                if (Image.IdImage == 0)
                {
                    Image.IdImage = NextKey("Images", "IdImage");
                    query = "INSERT INTO Images" +
                    " (idImage, imagePath, caption)" +
                    " Values (" + Image.IdImage + "," +
                    SqlString(Image.RelativePathAndFilename) + "," +
                    SqlString(Image.Caption) + "" +
                    ");";
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                }
                query = "INSERT INTO Lessons_Images" +
                    " (idImage, idLesson)" +
                    " Values (" + Image.IdImage + "," + Lesson.IdLesson + "" +
                    ");";
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();

                cmd.Dispose();
            }
            return Image.IdImage;
        }
    }
}
