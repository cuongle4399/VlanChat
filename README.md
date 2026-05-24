# VlanChat (LAN Chat Pro)

![Demo VlanChat](https://raw.githubusercontent.com/cuongle4399/cuongle4399/main/img/Screenshot%202026-05-18%20204704.png)

VlanChat là ứng dụng nhắn tin và chia sẻ file trong mạng nội bộ (LAN) được phát triển bằng C# WinForms (.NET). Ứng dụng dùng `LANChatServer` làm nút điều phối để quản lý danh sách người dùng online, chat nhóm, chat riêng và trạng thái nhập; dữ liệu file được truyền trực tiếp qua TCP từ máy gửi sang máy nhận để giữ tốc độ cao trong LAN.

## Tính năng chính

- Chat nhóm trong mạng nội bộ.
- Chat riêng giữa hai người dùng.
- Gửi icon/emoji.
- Gửi và nhận file, có hiển thị tiến độ truyền tải.
- Hiển thị danh sách người dùng online/offline.
- Cấu hình địa chỉ server LAN trong màn hình Settings.

## Cách chạy trong LAN

1. Chạy server trên một máy trong mạng:

   ```powershell
   dotnet run --project LANChatServer/LANChatServer.csproj -- 5000
   ```

2. Mở ứng dụng client trên các máy còn lại.
3. Vào Settings và nhập `Server Host` là IP LAN của máy đang chạy server, ví dụ `192.168.1.10`, port mặc định là `5000`.
