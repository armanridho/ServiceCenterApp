### 📁 `README.md`

```markdown
# 🔧 Service Center Management App

Aplikasi manajemen servis (Web & Desktop) untuk membantu proses pendaftaran, pemantauan, dan penyelesaian layanan perbaikan elektronik, laptop, HP, atau unit lainnya secara efisien dan terstruktur.

---

## 📦 Fitur Utama

### 🌐 Web Version (Frontend + Backend)
- Pendaftaran unit service
- Manajemen teknisi
- Status tracking dengan update real-time
- Notifikasi (WA/SMS - opsional)
- Login role-based: Admin, Teknisi
- Invoice otomatis (generate saat unit selesai)

### 💻 Windows Desktop Version (WPF)
- Dropdown pencarian unit (Brand & Tipe)
- Dropdown pencarian customer dari data tersimpan
- Dropdown status dengan logika otomatis:
  - `Masuk` → Isi `DateIn`
  - `Service` → Isi `ServiceDate`
  - `Keluar` → Isi `DateOut`
- DataGrid yang fix header-nya (tidak bisa digeser)

---

## 📂 Struktur Proyek


- ServiceCenter/
- ├── Web/
- │   ├── backend/         # PHP / Laravel / Node (menyesuaikan)
- │   └── frontend/        # HTML / Vue / React (menyesuaikan)
- ├── Windows/
- │   └── WPF_App/         # .NET Framework 4.8, C#
- ├── database/
- │   └── schema.sql       # Struktur database awal
- └── README.md


---

## 🚀 Cara Menjalankan

### Web Version
1. Clone repository ini
2. Jalankan backend di XAMPP / Apache / NodeJS server
3. Akses frontend dari browser

### Windows Version
1. Buka folder `Windows/WPF_App` di Visual Studio
2. Build dan Run
3. Aplikasi akan berjalan dalam mode desktop

---

## 📋 Catatan

- Database menggunakan MySQL/MariaDB (bisa disesuaikan)
- File konfigurasi berada di folder `config/`
- Untuk integrasi notifikasi WA/SMS, diperlukan setup API eksternal (disarankan pakai Wablas atau Twilio)

---

## 📄 License

**Copyright © 2025  
Arman Ridho**

This software is proprietary and all rights are reserved. Unauthorized use, modification, or distribution is strictly prohibited without permission.  
Contact for licensing: `admin@armanridho.my.id`

---

## 🙌 Kontribusi

Tidak menerima kontribusi publik saat ini. Untuk kolaborasi atau penggunaan komersial, silakan hubungi kontak di atas.
```

---
