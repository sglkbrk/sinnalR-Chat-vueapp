# sinnalR-Chat-vueapp

Frontend tarafında Vue.js, backend tarafında .NET kullanılarak inşa edilmiş bu proje, gerçek zamanlı iletişim için SignalR, dosya depolama için MinIO ve veri yönetimi için MySQL ile entegre edilmiştir. Tüm bu bileşenler Docker ve Docker Compose kullanılarak kolayca kurulabilir ve yönetilebilir.

## Özellikler

- **Frontend:** Vue.js
- **Backend:** .NET 
- **Gerçek Zamanlı İletişim:** SignalR entegrasyonu
- **Dosya Depolama:** MinIO 
- **Veritabanı:** MySQL 

## Teknolojiler

- **Frontend:** [Vue.js](https://vuejs.org/)
- **Backend:** [.NET](https://dotnet.microsoft.com/)
- **Gerçek Zamanlı İletişim:** [SignalR](https://dotnet.microsoft.com/apps/aspnet/signalr)
- **Dosya Depolama:** [MinIO](https://min.io/)
- **Veritabanı:** [MySQL](https://www.mysql.com/)
- **ORM:** [Entity Framework](https://docs.microsoft.com/en-us/ef/)
- **Containerization:** [Docker](https://www.docker.com/), [Docker Compose](https://docs.docker.com/compose/)

## Ön Koşullar

- [Docker](https://www.docker.com/get-started) yüklü olmalı
- [Docker Compose](https://docs.docker.com/compose/install/) yüklü olmalı

## Kurulum

   git clone 
   docker-compose up --build -d 
   dotnet ef migrations add InitialCreate --project ProductManagement.Infrastructure --startup-project ProductManagement.API
   dotnet ef   database  update --project ProductManagement.Infrastructure --startup-project ProductManagement.API
