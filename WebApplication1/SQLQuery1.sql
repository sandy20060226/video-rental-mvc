-- 1. 建立影片主檔資料表
CREATE TABLE TableVideos_1131619 (
    vNo INT IDENTITY(1,1) NOT NULL,
    title NVARCHAR(255) NOT NULL,
    type NVARCHAR(100) NULL,
    grade NVARCHAR(50) NULL,
    director NVARCHAR(100) NULL,
    CONSTRAINT PK_TableVideos_1131619 PRIMARY KEY CLUSTERED (vNo ASC)
);

-- 2. 建立會員資料表
CREATE TABLE TableMembers_1131619 (
    mId NVARCHAR(50) NOT NULL,
    name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(50) NULL,
    startDate DATETIME NULL,
    credits INT DEFAULT 0 NULL,
    address NVARCHAR(500) NULL,
    CONSTRAINT PK_TableMembers_1131619 PRIMARY KEY CLUSTERED (mId ASC)
);

-- 3. 建立影片拷貝資料表
CREATE TABLE TableVideoCopies_1131619 (
    cNo INT IDENTITY(1,1) NOT NULL,
    vNo INT NOT NULL,
    media NVARCHAR(50) NOT NULL,
    seq INT NULL,
    date DATETIME DEFAULT GETDATE() NULL,
    mId NVARCHAR(50) NULL,
    CONSTRAINT PK_TableVideoCopies_1131619 PRIMARY KEY CLUSTERED (cNo ASC),
    CONSTRAINT FK_VideoCopies_Videos_1131619 FOREIGN KEY (vNo) REFERENCES TableVideos_1131619 (vNo) ON DELETE CASCADE,
    CONSTRAINT FK_VideoCopies_Members_1131619 FOREIGN KEY (mId) REFERENCES TableMembers_1131619 (mId) ON UPDATE CASCADE ON DELETE SET NULL
);

-- 4. 灌入測試會員與影片資料
INSERT INTO TableMembers_1131619 (mId, name, phone, startDate, credits, address) VALUES 
('M001', '張小明', '0912345678', GETDATE(), 10, '台北市信義路一段1號'),
('M002', '李小美', '0922333444', GETDATE(), 25, '桃園市中壢區遠東路135號');

INSERT INTO TableVideos_1131619 (title, type, grade, director) VALUES 
('全面啟動 (Inception)', '科幻', '保護級', '克里斯多福·諾蘭'),
('阿凡達：水之道', '動作/冒險', '輔導級', '詹姆斯·卡麥隆'),
('鬼滅之刃劇場版', '動畫', '保護級', '外崎春雄');

INSERT INTO TableVideoCopies_1131619 (vNo, media, seq, date, mId) VALUES 
(1, 'Blue-ray', 1, GETDATE(), NULL),
(1, 'DVD', 2, GETDATE(), 'M001'),
(2, 'Blue-ray', 1, GETDATE(), NULL),
(3, 'DVD', 1, GETDATE(), NULL);