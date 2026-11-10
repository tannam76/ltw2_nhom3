-- Script tạo 100 User mẫu cho hệ thống CourseManagement
-- Mật khẩu mặc định cho toàn bộ user là: Password123@
-- Hash BCrypt: $2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy

SET NOCOUNT ON;

DECLARE @PasswordHash NVARCHAR(MAX) = N'$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy';
DECLARE @i INT = 1;
DECLARE @Email NVARCHAR(200);
DECLARE @FullName NVARCHAR(200);
DECLARE @Role NVARCHAR(50);
DECLARE @CreatedAt DATETIME2;

WHILE @i <= 100
BEGIN
    SET @Email = N'user' + RIGHT('000' + CAST(@i AS NVARCHAR(10)), 3) + N'@example.com';

    IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Email] = @Email)
    BEGIN
        -- Phân vai trò
        SET @Role = CASE 
            WHEN @i % 20 = 0 THEN N'Admin'
            WHEN @i % 20 IN (1, 2, 3) THEN N'Instructor'
            ELSE N'Student'
        END;

        -- Chọn tên mẫu
        SET @FullName = CASE (@i % 15)
            WHEN 0 THEN N'Nguyễn Văn ' + CASE (@i % 5) WHEN 0 THEN N'Anh' WHEN 1 THEN N'Dũng' WHEN 2 THEN N'Hải' WHEN 3 THEN N'Minh' ELSE N'Nam' END
            WHEN 1 THEN N'Trần Thị ' + CASE (@i % 5) WHEN 0 THEN N'Linh' WHEN 1 THEN N'Mai' WHEN 2 THEN N'Ngọc' WHEN 3 THEN N'Trang' ELSE N'Yến' END
            WHEN 2 THEN N'Lê Hoàng ' + CASE (@i % 5) WHEN 0 THEN N'Bình' WHEN 1 THEN N'Khánh' WHEN 2 THEN N'Long' WHEN 3 THEN N'Phúc' ELSE N'Quân' END
            WHEN 3 THEN N'Phạm Minh ' + CASE (@i % 5) WHEN 0 THEN N'Đức' WHEN 1 THEN N'Hiếu' WHEN 2 THEN N'Sơn' WHEN 3 THEN N'Thắng' ELSE N'Tùng' END
            WHEN 4 THEN N'Hoàng Đức ' + CASE (@i % 5) WHEN 0 THEN N'Anh' WHEN 1 THEN N'Huy' WHEN 2 THEN N'Kiên' WHEN 3 THEN N'Nam' ELSE N'Vinh' END
            WHEN 5 THEN N'Huỳnh Thanh ' + CASE (@i % 5) WHEN 0 THEN N'Hà' WHEN 1 THEN N'Phương' WHEN 2 THEN N'Thảo' WHEN 3 THEN N'Trúc' ELSE N'Vân' END
            WHEN 6 THEN N'Phan Quốc ' + CASE (@i % 5) WHEN 0 THEN N'Bảo' WHEN 1 THEN N'Cường' WHEN 2 THEN N'Đạt' WHEN 3 THEN N'Hưng' ELSE N'Khoa' END
            WHEN 7 THEN N'Vũ Hải ' + CASE (@i % 5) WHEN 0 THEN N'Đăng' WHEN 1 THEN N'Dương' WHEN 2 THEN N'Lâm' WHEN 3 THEN N'Phong' ELSE N'Tú' END
            WHEN 8 THEN N'Võ Gia ' + CASE (@i % 5) WHEN 0 THEN N'Bách' WHEN 1 THEN N'Hào' WHEN 2 THEN N'Khang' WHEN 3 THEN N'Phát' ELSE N'Thịnh' END
            WHEN 9 THEN N'Đặng Tuấn ' + CASE (@i % 5) WHEN 0 THEN N'Kiệt' WHEN 1 THEN N'Nghĩa' WHEN 2 THEN N'Sang' WHEN 3 THEN N'Tài' ELSE N'Trung' END
            WHEN 10 THEN N'Bùi Xuân ' + CASE (@i % 5) WHEN 0 THEN N'Hương' WHEN 1 THEN N'Lan' WHEN 2 THEN N'Oanh' WHEN 3 THEN N'Quỳnh' ELSE N'Thu' END
            WHEN 11 THEN N'Đỗ Hữu ' + CASE (@i % 5) WHEN 0 THEN N'Bằng' WHEN 1 THEN N'Chiến' WHEN 2 THEN N'Đoàn' WHEN 3 THEN N'Hòa' ELSE N'Luân' END
            WHEN 12 THEN N'Hồ Ngọc ' + CASE (@i % 5) WHEN 0 THEN N'Diệp' WHEN 1 THEN N'Hân' WHEN 2 THEN N'My' WHEN 3 THEN N'Nhi' ELSE N'Vy' END
            WHEN 13 THEN N'Ngô Quang ' + CASE (@i % 5) WHEN 0 THEN N'Hải' WHEN 1 THEN N'Huy' WHEN 2 THEN N'Minh' WHEN 3 THEN N'Thành' ELSE N'Vũ' END
            ELSE N'Dương Trọng ' + CASE (@i % 5) WHEN 0 THEN N'Hiếu' WHEN 1 THEN N'Nhân' WHEN 2 THEN N'Tín' WHEN 3 THEN N'Trí' ELSE N'Việt' END
        END;

        -- Thời gian tạo trải đều trong 90 ngày
        SET @CreatedAt = DATEADD(DAY, -(@i % 90), GETUTCDATE());

        INSERT INTO [dbo].[Users] ([FullName], [Email], [PasswordHash], [Role], [CreatedAt])
        VALUES (@FullName, @Email, @PasswordHash, @Role, @CreatedAt);
    END;

    SET @i = @i + 1;
END;

SELECT COUNT(*) AS [TotalUsers] FROM [dbo].[Users];
