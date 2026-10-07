CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `QuizManagers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Username` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Created` datetime(6) NOT NULL,
    CONSTRAINT `PK_QuizManagers` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Quizzes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Title` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(1000) CHARACTER SET utf8mb4 NULL,
    `IsActive` tinyint(1) NOT NULL,
    `Created` datetime(6) NOT NULL,
    CONSTRAINT `PK_Quizzes` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Questions` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `QuizId` int NOT NULL,
    `Text` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `Type` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `Points` int NOT NULL,
    `TimeLimitSeconds` int NULL,
    CONSTRAINT `PK_Questions` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Questions_Quizzes_QuizId` FOREIGN KEY (`QuizId`) REFERENCES `Quizzes` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `QuizResults` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `QuizId` int NOT NULL,
    `ParticipantName` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `Email` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Phone` varchar(200) CHARACTER SET utf8mb4 NULL,
    `Score` int NOT NULL,
    `TotalPoints` int NOT NULL,
    `CompletedAt` datetime(6) NOT NULL,
    CONSTRAINT `PK_QuizResults` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_QuizResults_Quizzes_QuizId` FOREIGN KEY (`QuizId`) REFERENCES `Quizzes` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `AnswerChoices` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `QuestionId` int NOT NULL,
    `Text` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `IsCorrect` tinyint(1) NOT NULL,
    `OrderIndex` int NOT NULL,
    `ImageUrl` varchar(500) CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_AnswerChoices` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_AnswerChoices_Questions_QuestionId` FOREIGN KEY (`QuestionId`) REFERENCES `Questions` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `Answers` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `QuestionId` int NOT NULL,
    `QuizResultId` int NULL,
    `GivenText` varchar(1000) CHARACTER SET utf8mb4 NOT NULL,
    `IsCorrect` tinyint(1) NOT NULL,
    `Score` int NOT NULL,
    CONSTRAINT `PK_Answers` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Answers_Questions_QuestionId` FOREIGN KEY (`QuestionId`) REFERENCES `Questions` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_Answers_QuizResults_QuizResultId` FOREIGN KEY (`QuizResultId`) REFERENCES `QuizResults` (`Id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_AnswerChoices_QuestionId_OrderIndex` ON `AnswerChoices` (`QuestionId`, `OrderIndex`);

CREATE INDEX `IX_Answers_QuestionId` ON `Answers` (`QuestionId`);

CREATE INDEX `IX_Answers_QuizResultId` ON `Answers` (`QuizResultId`);

CREATE INDEX `IX_Questions_QuizId` ON `Questions` (`QuizId`);

CREATE UNIQUE INDEX `IX_QuizManagers_Username` ON `QuizManagers` (`Username`);

CREATE INDEX `IX_QuizResults_QuizId` ON `QuizResults` (`QuizId`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261007222645_InitialCreate', '8.0.8');

COMMIT;

