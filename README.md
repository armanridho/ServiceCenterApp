### 📁 `README.md`

```markdown
# 🔧 Service Center Management App

Aplikasi manajemen servis (Web & Desktop) untuk membantu proses
- pendaftaran,
- pemantauan, dan
- penyelesaian layanan perbaikan Barang (tidak terkait dengan segment apapun)

---

## 📦 Fitur Utama

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
## Cara Menjalankan Aplikasi

### Windows Version
1. Download Release dan extract
2. Run file .exe
3. Aplikasi akan berjalan dalam mode desktop


## Cara Build Project

### Windows Version
1. Clone This Project
2. Buka foldernya
4. Jalankan file .sln
2. Build dan Run
3. Aplikasi akan berjalan dalam mode desktop

---

## 📋 Catatan

- Database menggunakan SQLite

---

## 📄 License

**Copyright © 2025  
Arman Ridho maulana**

This software is proprietary and all rights are reserved. This is Free and Not For Sale. 
Always respect copyright if you re-publish or modify app

---
```

---
