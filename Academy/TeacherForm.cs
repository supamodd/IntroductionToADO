using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;
using System.IO;
using DBtools;
using Academy.Models;

namespace Academy
{
    public partial class TeacherForm : HumanForm
    {
        private Connector connector;
        private byte[] currentPhotoBytes = null;
        private bool _saved = false;
        private int currentId = 0;

        public TeacherForm()
        {
            InitializeComponent();
            connector = new Connector(ConfigurationManager.ConnectionStrings["SPU_411_Import"].ConnectionString);
            pictureBoxPhoto.SizeMode = PictureBoxSizeMode.Zoom;
            buttonPhoto.Click += buttonPhoto_Click;
            buttonOK.Click += buttonOK_Click;
        }

        public TeacherForm(int id) : this()
        {
            currentId = id;
            DataTable dt = connector.Select("*", "Teachers", $"teacher_id={id}");

            if (dt.Rows.Count > 0)
            {
                rtbLastName.Text = dt.Rows[0]["last_name"].ToString();
                rtbFirstName.Text = dt.Rows[0]["first_name"].ToString();
                rtbMiddleName.Text = dt.Rows[0]["middle_name"].ToString();
                dtpBirthDate.Value = Convert.ToDateTime(dt.Rows[0]["birth_date"]);
                dtpWorkSince.Value = Convert.ToDateTime(dt.Rows[0]["work_since"]);

                pictureBoxPhoto.Image = connector.DownloadPhoto(id, "Teachers", "photo");
            }
        }

        private void buttonPhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Выберите фото преподавателя";
                ofd.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        using (Image original = Image.FromFile(ofd.FileName))
                        {
                            Image thumbnail = ResizeToThumbnail(original, 800, 800);
                            pictureBoxPhoto.Image?.Dispose();
                            pictureBoxPhoto.Image = thumbnail;

                            using (MemoryStream ms = new MemoryStream())
                            {
                                var jpegEncoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                                var encParams = new EncoderParameters(1);
                                encParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
                                thumbnail.Save(ms, jpegEncoder, encParams);
                                currentPhotoBytes = ms.ToArray();
                            }

                            MessageBox.Show($"Фото уменьшено до {thumbnail.Width}×{thumbnail.Height}\nРазмер: {currentPhotoBytes.Length / 1024} КБ",
                                "Миниатюра готова", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось загрузить фото:\n" + ex.Message, "Ошибка",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private Image ResizeToThumbnail(Image original, int maxWidth, int maxHeight)
        {
            double ratio = Math.Min((double)maxWidth / original.Width, (double)maxHeight / original.Height);
            int newWidth = (int)(original.Width * ratio);
            int newHeight = (int)(original.Height * ratio);
            if (newWidth == original.Width && newHeight == original.Height)
                return original;

            Bitmap thumbnail = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(thumbnail))
            {
                g.Clear(Color.White);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            return thumbnail;
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (_saved) return;
            _saved = true;

            if (string.IsNullOrWhiteSpace(rtbLastName.Text) || string.IsNullOrWhiteSpace(rtbFirstName.Text))
            {
                MessageBox.Show("Заполните фамилию и имя преподавателя!", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Teacher teacher = new Teacher(
                currentId,
                rtbLastName.Text.Trim(),
                rtbFirstName.Text.Trim(),
                rtbMiddleName.Text?.Trim() ?? "",
                dtpBirthDate.Value.ToString("yyyy-MM-dd"),
                dtpWorkSince.Value.ToString("yyyy-MM-dd"),
                pictureBoxPhoto.Image
            );

            if (teacher.id == 0)   // добавление нового
            {
                teacher.id = connector.GetNextPrimaryKey("Teachers");                    // ← вот это отличие от студентов
                connector.Insert($"INSERT Teachers({teacher.GetNames()}) VALUES ({teacher})");
            }
            else                   // редактирование
            {
                connector.Update($"UPDATE Teachers SET {teacher.ToStringUpdate()} WHERE teacher_id={teacher.id}");
            }

            // фото (точно как у студентов)
            if (pictureBoxPhoto.Image != null)
                connector.UploadPhoto(teacher.SerializePhoto(), teacher.id, "photo", "Teachers");

            MessageBox.Show(teacher.id == 0
                ? "Преподаватель успешно добавлен!"
                : "Преподаватель успешно обновлён!",
                "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}