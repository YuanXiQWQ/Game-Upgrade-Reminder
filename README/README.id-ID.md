# Pengingat Peningkatan Game

[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](https://opensource.org/licenses/AGPL-3.0)

---

Sebuah alat untuk mencatat dan melacak kemajuan peningkatan game yang membutuhkan banyak waktu. Awalnya dibuat untuk **Boom Beach**.

## Fitur

- 🕒 Melacak tugas peningkatan pada beberapa akun
- ⏰ Berbeda dengan kalender/alarm, hitungan mundur disinkronkan dengan game, menghilangkan kebutuhan menghitung waktu secara manual setiap kali
- 🔔 Menampilkan notifikasi sistem saat peningkatan selesai
- ♻️ Tugas berulang: harian / mingguan / bulanan / tahunan / kustom; waktu selesai opsional (default: tidak ada); mendukung aturan lewati
- 🌐 Mendukung 27 bahasa

## Persyaratan Sistem

- [Windows 10](https://www.microsoft.com/en-ca/software-download/windows10) atau yang lebih baru
- [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) atau yang lebih baru

> Tidak yakin apakah versi lain akan berfungsi :<

## Instalasi

1. Unduh versi terbaru dari halaman [Releases](https://github.com/YuanXiQWQ/Game-Upgrade-Reminder/releases)
2. Ekstrak ke folder mana saja
3. Jalankan `Game Upgrade Reminder.exe`

## Penggunaan

### Menambahkan tugas peningkatan

1. Pilih akun di bagian atas antarmuka
2. Pilih atau buat nama tugas (boleh dikosongkan)
3. Atur waktu yang diperlukan: waktu mulai, hari, jam, menit (jika tidak diatur, waktu mulai default adalah waktu sistem saat ini)
4. Klik tombol "Tambah" untuk membuat tugas

### Mengelola tugas

- Tugas yang jatuh tempo akan disorot; klik "Selesai" untuk menandainya sebagai selesai
- Tugas dapat dihapus dari daftar, dan penghapusan dapat dibatalkan dalam waktu tiga detik

## FAQ

### Tidak menerima notifikasi sistem

- Matikan **Focus Assist**, atau tambahkan `Game Upgrade Reminder.exe` ke daftar prioritas. Jika aturan otomatis disetel ke "Hanya alarm", ubah menjadi "Hanya prioritas".
- Selain itu saya tidak tahu

### Masalah aneh lainnya

- Mungkin itu bug, abaikan saja
- Bisa dilaporkan di halaman Issues, tetapi kemungkinan besar saya tidak tahu cara memperbaikinya

## Lisensi

Proyek ini dilisensikan di bawah [GNU Affero General Public License v3.0](../LICENSE).