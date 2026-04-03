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

namespace Academy
{
    public partial class TeacherForm : HumanForm
    {
        private Connector connector;
        private byte[] currentPhotoBytes = null;
        private bool _saved = false;

        public TeacherForm()
        {
            InitializeComponent();

            connector = new Connector(ConfigurationManager.ConnectionStrings["SPU_411_Import"].ConnectionString);

            pictureBoxPhoto.SizeMode = PictureBoxSizeMode.Zoom;
            buttonPhoto.Click += buttonPhoto_Click;
            buttonOK.Click += buttonOK_Click; 
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
                                var jpegEncoder = ImageCodecInfo.GetImageEncoders()
                                    .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

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

            if (string.IsNullOrWhiteSpace(rtbLastName.Text) ||
                string.IsNullOrWhiteSpace(rtbFirstName.Text))
            {
                MessageBox.Show("Заполните фамилию и имя преподавателя!",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int teacherId = connector.GetNextPrimaryKey("Teachers");

            string columns = "teacher_id, last_name, first_name, middle_name, birth_date, work_since, photo";
            string values =
                $"{teacherId}," +
                $"N'{rtbLastName.Text.Trim()}'," +
                $"N'{rtbFirstName.Text.Trim()}'," +
                $"N'{rtbMiddleName.Text?.Trim() ?? ""}'," +
                $"N'{dtpBirthDate.Value:yyyy-MM-dd}'," +
                $"N'{dtpWorkSince.Value:yyyy-MM-dd}',";

            if (currentPhotoBytes != null && currentPhotoBytes.Length > 0)
            {
                string hex = "0x" + BitConverter.ToString(currentPhotoBytes).Replace("-", "");
                values += hex;
            }
            else
            {
                values += "NULL";
            }

            try
            {
                connector.Insert("Teachers", columns, values);

                MessageBox.Show("Преподаватель успешно добавлен!", "Успех",
                               MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении преподавателя:\n" + ex.Message,
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}