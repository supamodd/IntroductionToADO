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
    public partial class StudentForm : HumanForm
    {
        private Dictionary<string, int> d_groups;
        private Connector connector;
        private byte[] currentPhotoBytes = null;

        public StudentForm()
        {
            InitializeComponent();

            connector = new Connector(ConfigurationManager.ConnectionStrings["SPU_411_Import"].ConnectionString);

            d_groups = connector.LoadDictionary("Groups");
            cbStudentsGroup.Items.AddRange(d_groups.Keys.ToArray());

            if (cbStudentsGroup.Items.Count > 0)
                cbStudentsGroup.SelectedIndex = 0;

            pictureBoxPhoto.SizeMode = PictureBoxSizeMode.Zoom;
            buttonPhoto.Click += buttonPhoto_Click;
        }

        private void buttonPhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Выберите фото студента";
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

                            MessageBox.Show(
                                $"Оригинал: {original.Width}×{original.Height}\n" +
                                $"Миниатюра: {thumbnail.Width}×{thumbnail.Height}\n" +
                                $"Размер в БД: {currentPhotoBytes.Length / 1024} КБ",
                                "Фото уменьшено ✓", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (string.IsNullOrWhiteSpace(rtbLastName.Text) ||
                string.IsNullOrWhiteSpace(rtbFirstName.Text) ||
                cbStudentsGroup.SelectedIndex == -1)
            {
                MessageBox.Show("Заполните фамилию, имя и выберите группу!",
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int groupId = d_groups[cbStudentsGroup.SelectedItem.ToString()];

            string columns = "last_name,first_name,middle_name,birth_date,[group],photo";
            string values =
                $"N'{rtbLastName.Text.Trim()}'," +
                $"N'{rtbFirstName.Text.Trim()}'," +
                $"N'{rtbMiddleName.Text?.Trim() ?? ""}'," +
                $"N'{dtpBirthDate.Value:yyyy-MM-dd}'," +
                $"{groupId},";

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
                connector.Insert("Students", columns, values);

                MessageBox.Show("Студент успешно добавлен с фото!", "Успех",
                               MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при добавлении студента:\n" + ex.Message,
                               "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}