-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Host: 127.0.0.1
-- Generation Time: May 04, 2025 at 04:26 PM
-- Server version: 10.4.32-MariaDB
-- PHP Version: 8.2.12

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Database: `homeowners_db`
--
CREATE DATABASE IF NOT EXISTS `homeowners_db` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE `homeowners_db`;

-- --------------------------------------------------------

--
-- Table structure for table `addresses`
--

DROP TABLE IF EXISTS `addresses`;
CREATE TABLE `addresses` (
  `AddressId` int(11) NOT NULL,
  `UserId` int(11) NOT NULL,
  `BlockNumber` varchar(50) NOT NULL,
  `LotNumber` varchar(50) NOT NULL,
  `StreetName` varchar(100) NOT NULL,
  `Phase` varchar(50) NOT NULL,
  `AdditionalDetails` text DEFAULT NULL,
  `DateAdded` datetime NOT NULL DEFAULT current_timestamp(),
  `LastModified` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `announcements`
--

DROP TABLE IF EXISTS `announcements`;
CREATE TABLE `announcements` (
  `AnnouncementID` int(11) NOT NULL,
  `Title` varchar(255) NOT NULL,
  `Content` text NOT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp(),
  `UserID` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `bills`
--

DROP TABLE IF EXISTS `bills`;
CREATE TABLE `bills` (
  `BillID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `BillTypeID` int(11) NOT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `DueDate` date NOT NULL,
  `Status` enum('Pending','Paid','Overdue','Cancelled') NOT NULL DEFAULT 'Pending',
  `Description` text DEFAULT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp(),
  `UpdatedAt` timestamp NULL DEFAULT NULL ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `bill_types`
--

DROP TABLE IF EXISTS `bill_types`;
CREATE TABLE `bill_types` (
  `BillTypeID` int(11) NOT NULL,
  `Name` varchar(100) NOT NULL,
  `Description` text DEFAULT NULL,
  `IsRecurring` tinyint(1) DEFAULT 0,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `contact_directory`
--

DROP TABLE IF EXISTS `contact_directory`;
CREATE TABLE `contact_directory` (
  `ContactID` int(11) NOT NULL,
  `Name` varchar(255) NOT NULL,
  `Position` varchar(255) NOT NULL,
  `Department` varchar(100) NOT NULL,
  `PhoneNumber` varchar(20) DEFAULT NULL,
  `Email` varchar(255) DEFAULT NULL,
  `OfficeLocation` varchar(255) DEFAULT NULL,
  `OfficeHours` varchar(255) DEFAULT NULL,
  `CreatedAt` datetime DEFAULT current_timestamp(),
  `UpdatedAt` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `documents`
--

DROP TABLE IF EXISTS `documents`;
CREATE TABLE `documents` (
  `DocumentID` int(11) NOT NULL,
  `Title` varchar(255) NOT NULL,
  `FilePath` varchar(255) NOT NULL,
  `UploadedBy` int(11) DEFAULT NULL,
  `UploadedAt` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `facilities`
--

DROP TABLE IF EXISTS `facilities`;
CREATE TABLE `facilities` (
  `FacilityID` int(11) NOT NULL,
  `FacilityName` varchar(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `facility_reservations`
--

DROP TABLE IF EXISTS `facility_reservations`;
CREATE TABLE `facility_reservations` (
  `ReservationID` int(11) NOT NULL,
  `UserID` int(11) DEFAULT NULL,
  `ReservationDate` date NOT NULL,
  `StartTime` time NOT NULL,
  `EndTime` time NOT NULL,
  `Status` enum('Pending','Confirmed','Cancelled') NOT NULL,
  `FacilityID` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `feedback`
--

DROP TABLE IF EXISTS `feedback`;
CREATE TABLE `feedback` (
  `FeedbackID` int(11) NOT NULL,
  `UserID` int(11) DEFAULT NULL,
  `Title` varchar(255) DEFAULT NULL,
  `Content` text DEFAULT NULL,
  `Status` varchar(50) DEFAULT 'Pending',
  `CreatedAt` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `forum_comments`
--

DROP TABLE IF EXISTS `forum_comments`;
CREATE TABLE `forum_comments` (
  `CommentID` int(11) NOT NULL,
  `PostID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Content` text NOT NULL,
  `Image` blob DEFAULT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `forum_comment_votes`
--

DROP TABLE IF EXISTS `forum_comment_votes`;
CREATE TABLE `forum_comment_votes` (
  `VoteID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `CommentID` int(11) NOT NULL,
  `VoteValue` tinyint(4) NOT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `forum_posts`
--

DROP TABLE IF EXISTS `forum_posts`;
CREATE TABLE `forum_posts` (
  `PostID` int(11) NOT NULL,
  `UserID` int(11) DEFAULT NULL,
  `Title` varchar(255) NOT NULL,
  `Content` text NOT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp(),
  `Image` blob DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `forum_post_votes`
--

DROP TABLE IF EXISTS `forum_post_votes`;
CREATE TABLE `forum_post_votes` (
  `VoteID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `PostID` int(11) NOT NULL,
  `VoteValue` tinyint(4) NOT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `payments`
--

DROP TABLE IF EXISTS `payments`;
CREATE TABLE `payments` (
  `PaymentID` int(11) NOT NULL,
  `BillID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `Amount` decimal(10,2) NOT NULL,
  `PaymentDate` datetime NOT NULL,
  `PaymentMethod` varchar(50) NOT NULL,
  `ReferenceNumber` varchar(100) DEFAULT NULL,
  `ProofOfPayment` longblob DEFAULT NULL,
  `Status` enum('Pending','Verified','Rejected') NOT NULL DEFAULT 'Pending',
  `Notes` text DEFAULT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp(),
  `VerifiedAt` timestamp NULL DEFAULT NULL,
  `VerifiedBy` int(11) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `payment_history`
--

DROP TABLE IF EXISTS `payment_history`;
CREATE TABLE `payment_history` (
  `HistoryID` int(11) NOT NULL,
  `PaymentID` int(11) NOT NULL,
  `Action` varchar(50) NOT NULL,
  `PreviousStatus` varchar(50) DEFAULT NULL,
  `NewStatus` varchar(50) DEFAULT NULL,
  `Notes` text DEFAULT NULL,
  `PerformedBy` int(11) NOT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8 COLLATE=utf8_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `polls`
--

DROP TABLE IF EXISTS `polls`;
CREATE TABLE `polls` (
  `PollID` int(11) NOT NULL,
  `Title` varchar(255) NOT NULL,
  `Description` text DEFAULT NULL,
  `StartDate` datetime NOT NULL,
  `EndDate` datetime NOT NULL,
  `CreatedBy` int(11) NOT NULL,
  `CreatedAt` datetime DEFAULT current_timestamp(),
  `UpdatedAt` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `poll_options`
--

DROP TABLE IF EXISTS `poll_options`;
CREATE TABLE `poll_options` (
  `OptionID` int(11) NOT NULL,
  `PollID` int(11) NOT NULL,
  `OptionText` varchar(255) NOT NULL,
  `CreatedAt` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `poll_responses`
--

DROP TABLE IF EXISTS `poll_responses`;
CREATE TABLE `poll_responses` (
  `ResponseID` int(11) NOT NULL,
  `PollID` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `OptionID` int(11) NOT NULL,
  `CreatedAt` datetime DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `service_requests`
--

DROP TABLE IF EXISTS `service_requests`;
CREATE TABLE `service_requests` (
  `RequestID` int(11) NOT NULL,
  `UserID` int(11) DEFAULT NULL,
  `Description` text NOT NULL,
  `Status` enum('Pending','In Progress','Resolved','Rejected') NOT NULL,
  `RequestDate` timestamp NOT NULL DEFAULT current_timestamp(),
  `ServiceSchedule` datetime DEFAULT NULL,
  `ServiceTypeID` int(11) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `service_types`
--

DROP TABLE IF EXISTS `service_types`;
CREATE TABLE `service_types` (
  `ServiceTypeID` int(11) NOT NULL,
  `ServiceTypeName` varchar(100) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
CREATE TABLE `users` (
  `UserID` int(11) NOT NULL,
  `Username` varchar(50) NOT NULL,
  `PasswordHash` varchar(255) NOT NULL,
  `Email` varchar(100) NOT NULL,
  `PhoneNumber` varchar(20) DEFAULT NULL,
  `Role` enum('Homeowner','Administrator','Staff') NOT NULL,
  `CreatedAt` timestamp NOT NULL DEFAULT current_timestamp(),
  `FirstName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `MiddleName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `LastName` varchar(50) CHARACTER SET utf8 COLLATE utf8_general_ci DEFAULT NULL,
  `Address` varchar(200) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Table structure for table `vehicleregistrations`
--

DROP TABLE IF EXISTS `vehicleregistrations`;
CREATE TABLE `vehicleregistrations` (
  `VehicleId` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `PlateNumber` varchar(20) NOT NULL,
  `VehicleModel` varchar(50) NOT NULL,
  `VehicleColor` varchar(20) NOT NULL,
  `VehicleType` varchar(20) NOT NULL,
  `VehicleBrand` varchar(50) NOT NULL,
  `RegistrationDate` datetime NOT NULL DEFAULT current_timestamp(),
  `ExpiryDate` date NOT NULL,
  `Status` enum('Active','Expired','Suspended') NOT NULL DEFAULT 'Active',
  `Notes` text DEFAULT NULL,
  `State` enum('Pending','Accepted','Rejected') NOT NULL DEFAULT 'Pending',
  `RejectionReason` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `visitorpasses`
--

DROP TABLE IF EXISTS `visitorpasses`;
CREATE TABLE `visitorpasses` (
  `VisitorPassId` int(11) NOT NULL,
  `UserID` int(11) NOT NULL,
  `VisitorName` varchar(100) NOT NULL,
  `VisitorContact` varchar(20) NOT NULL,
  `Purpose` varchar(50) NOT NULL,
  `VisitDate` date NOT NULL,
  `VisitTime` time NOT NULL,
  `ExpiryDate` date NOT NULL,
  `Status` enum('Pending','Approved','Rejected','Expired') NOT NULL DEFAULT 'Pending',
  `VehiclePlateNumber` varchar(20) DEFAULT NULL,
  `VehicleModel` varchar(50) DEFAULT NULL,
  `VehicleColor` varchar(20) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `ApprovedAt` datetime DEFAULT NULL,
  `ApprovedBy` int(11) DEFAULT NULL,
  `RejectionReason` text DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Table structure for table `visitor_passes`
--

DROP TABLE IF EXISTS `visitor_passes`;
CREATE TABLE `visitor_passes` (
  `VisitorPassId` int(11) NOT NULL,
  `UserID` int(11) DEFAULT NULL,
  `VisitorName` varchar(100) NOT NULL,
  `VisitorContact` varchar(20) NOT NULL,
  `Purpose` varchar(50) NOT NULL,
  `VisitDate` date NOT NULL,
  `VisitTime` time NOT NULL,
  `ExpiryDate` date NOT NULL,
  `Status` varchar(20) NOT NULL DEFAULT 'Pending',
  `VehiclePlateNumber` varchar(20) DEFAULT NULL,
  `VehicleModel` varchar(50) DEFAULT NULL,
  `VehicleColor` varchar(20) DEFAULT NULL,
  `CreatedAt` datetime NOT NULL DEFAULT current_timestamp(),
  `ApprovedAt` datetime DEFAULT NULL,
  `ApprovedBy` int(11) DEFAULT NULL,
  `RejectionReason` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Indexes for dumped tables
--

--
-- Indexes for table `addresses`
--
ALTER TABLE `addresses`
  ADD PRIMARY KEY (`AddressId`),
  ADD KEY `FK_Addresses_Users` (`UserId`);

--
-- Indexes for table `announcements`
--
ALTER TABLE `announcements`
  ADD PRIMARY KEY (`AnnouncementID`),
  ADD KEY `UserID` (`UserID`);

--
-- Indexes for table `bills`
--
ALTER TABLE `bills`
  ADD PRIMARY KEY (`BillID`),
  ADD KEY `UserID` (`UserID`),
  ADD KEY `BillTypeID` (`BillTypeID`);

--
-- Indexes for table `bill_types`
--
ALTER TABLE `bill_types`
  ADD PRIMARY KEY (`BillTypeID`);

--
-- Indexes for table `contact_directory`
--
ALTER TABLE `contact_directory`
  ADD PRIMARY KEY (`ContactID`);

--
-- Indexes for table `documents`
--
ALTER TABLE `documents`
  ADD PRIMARY KEY (`DocumentID`),
  ADD KEY `UploadedBy` (`UploadedBy`);

--
-- Indexes for table `facilities`
--
ALTER TABLE `facilities`
  ADD PRIMARY KEY (`FacilityID`);

--
-- Indexes for table `facility_reservations`
--
ALTER TABLE `facility_reservations`
  ADD PRIMARY KEY (`ReservationID`),
  ADD KEY `UserID` (`UserID`),
  ADD KEY `FacilityID` (`FacilityID`);

--
-- Indexes for table `feedback`
--
ALTER TABLE `feedback`
  ADD PRIMARY KEY (`FeedbackID`),
  ADD KEY `UserID` (`UserID`);

--
-- Indexes for table `forum_comments`
--
ALTER TABLE `forum_comments`
  ADD PRIMARY KEY (`CommentID`),
  ADD KEY `PostID_idx` (`PostID`),
  ADD KEY `fk_forum_comments_users` (`UserID`);

--
-- Indexes for table `forum_comment_votes`
--
ALTER TABLE `forum_comment_votes`
  ADD PRIMARY KEY (`VoteID`),
  ADD UNIQUE KEY `UniqueUserCommentVote` (`UserID`,`CommentID`),
  ADD KEY `CommentID_idx` (`CommentID`);

--
-- Indexes for table `forum_posts`
--
ALTER TABLE `forum_posts`
  ADD PRIMARY KEY (`PostID`),
  ADD KEY `UserID` (`UserID`);

--
-- Indexes for table `forum_post_votes`
--
ALTER TABLE `forum_post_votes`
  ADD PRIMARY KEY (`VoteID`),
  ADD UNIQUE KEY `UniqueUserPostVote` (`UserID`,`PostID`),
  ADD KEY `PostID_idx` (`PostID`);

--
-- Indexes for table `payments`
--
ALTER TABLE `payments`
  ADD PRIMARY KEY (`PaymentID`),
  ADD KEY `BillID` (`BillID`),
  ADD KEY `UserID` (`UserID`),
  ADD KEY `VerifiedBy` (`VerifiedBy`);

--
-- Indexes for table `payment_history`
--
ALTER TABLE `payment_history`
  ADD PRIMARY KEY (`HistoryID`),
  ADD KEY `PaymentID` (`PaymentID`),
  ADD KEY `PerformedBy` (`PerformedBy`);

--
-- Indexes for table `polls`
--
ALTER TABLE `polls`
  ADD PRIMARY KEY (`PollID`),
  ADD KEY `CreatedBy` (`CreatedBy`);

--
-- Indexes for table `poll_options`
--
ALTER TABLE `poll_options`
  ADD PRIMARY KEY (`OptionID`),
  ADD KEY `PollID` (`PollID`);

--
-- Indexes for table `poll_responses`
--
ALTER TABLE `poll_responses`
  ADD PRIMARY KEY (`ResponseID`),
  ADD UNIQUE KEY `unique_response` (`PollID`,`UserID`),
  ADD KEY `UserID` (`UserID`),
  ADD KEY `OptionID` (`OptionID`);

--
-- Indexes for table `service_requests`
--
ALTER TABLE `service_requests`
  ADD PRIMARY KEY (`RequestID`),
  ADD KEY `UserID` (`UserID`),
  ADD KEY `ServiceTypeID` (`ServiceTypeID`);

--
-- Indexes for table `service_types`
--
ALTER TABLE `service_types`
  ADD PRIMARY KEY (`ServiceTypeID`);

--
-- Indexes for table `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`UserID`),
  ADD UNIQUE KEY `Username` (`Username`),
  ADD UNIQUE KEY `Email` (`Email`);

--
-- Indexes for table `vehicleregistrations`
--
ALTER TABLE `vehicleregistrations`
  ADD PRIMARY KEY (`VehicleId`),
  ADD UNIQUE KEY `UQ_PlateNumber` (`PlateNumber`),
  ADD KEY `IX_VehicleRegistrations_UserID` (`UserID`),
  ADD KEY `IX_VehicleRegistrations_Status` (`Status`),
  ADD KEY `IX_VehicleRegistrations_ExpiryDate` (`ExpiryDate`);

--
-- Indexes for table `visitorpasses`
--
ALTER TABLE `visitorpasses`
  ADD PRIMARY KEY (`VisitorPassId`),
  ADD KEY `ApprovedBy` (`ApprovedBy`),
  ADD KEY `IX_VisitorPasses_UserID` (`UserID`),
  ADD KEY `IX_VisitorPasses_Status` (`Status`),
  ADD KEY `IX_VisitorPasses_VisitDate` (`VisitDate`);

--
-- Indexes for table `visitor_passes`
--
ALTER TABLE `visitor_passes`
  ADD PRIMARY KEY (`VisitorPassId`),
  ADD KEY `UserID` (`UserID`);

--
-- AUTO_INCREMENT for dumped tables
--

--
-- AUTO_INCREMENT for table `addresses`
--
ALTER TABLE `addresses`
  MODIFY `AddressId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `announcements`
--
ALTER TABLE `announcements`
  MODIFY `AnnouncementID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `bills`
--
ALTER TABLE `bills`
  MODIFY `BillID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `bill_types`
--
ALTER TABLE `bill_types`
  MODIFY `BillTypeID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `contact_directory`
--
ALTER TABLE `contact_directory`
  MODIFY `ContactID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `documents`
--
ALTER TABLE `documents`
  MODIFY `DocumentID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `facilities`
--
ALTER TABLE `facilities`
  MODIFY `FacilityID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `facility_reservations`
--
ALTER TABLE `facility_reservations`
  MODIFY `ReservationID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `feedback`
--
ALTER TABLE `feedback`
  MODIFY `FeedbackID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `forum_comments`
--
ALTER TABLE `forum_comments`
  MODIFY `CommentID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `forum_comment_votes`
--
ALTER TABLE `forum_comment_votes`
  MODIFY `VoteID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `forum_posts`
--
ALTER TABLE `forum_posts`
  MODIFY `PostID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `forum_post_votes`
--
ALTER TABLE `forum_post_votes`
  MODIFY `VoteID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `payments`
--
ALTER TABLE `payments`
  MODIFY `PaymentID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `payment_history`
--
ALTER TABLE `payment_history`
  MODIFY `HistoryID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `polls`
--
ALTER TABLE `polls`
  MODIFY `PollID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `poll_options`
--
ALTER TABLE `poll_options`
  MODIFY `OptionID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `poll_responses`
--
ALTER TABLE `poll_responses`
  MODIFY `ResponseID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `service_requests`
--
ALTER TABLE `service_requests`
  MODIFY `RequestID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `service_types`
--
ALTER TABLE `service_types`
  MODIFY `ServiceTypeID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `users`
--
ALTER TABLE `users`
  MODIFY `UserID` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `vehicleregistrations`
--
ALTER TABLE `vehicleregistrations`
  MODIFY `VehicleId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `visitorpasses`
--
ALTER TABLE `visitorpasses`
  MODIFY `VisitorPassId` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT for table `visitor_passes`
--
ALTER TABLE `visitor_passes`
  MODIFY `VisitorPassId` int(11) NOT NULL AUTO_INCREMENT;

--
-- Constraints for dumped tables
--

--
-- Constraints for table `addresses`
--
ALTER TABLE `addresses`
  ADD CONSTRAINT `FK_Addresses_Users` FOREIGN KEY (`UserId`) REFERENCES `users` (`UserID`);

--
-- Constraints for table `bills`
--
ALTER TABLE `bills`
  ADD CONSTRAINT `bills_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE,
  ADD CONSTRAINT `bills_ibfk_2` FOREIGN KEY (`BillTypeID`) REFERENCES `bill_types` (`BillTypeID`) ON DELETE CASCADE;

--
-- Constraints for table `documents`
--
ALTER TABLE `documents`
  ADD CONSTRAINT `documents_ibfk_1` FOREIGN KEY (`UploadedBy`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- Constraints for table `facility_reservations`
--
ALTER TABLE `facility_reservations`
  ADD CONSTRAINT `facility_reservations_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE,
  ADD CONSTRAINT `facility_reservations_ibfk_2` FOREIGN KEY (`FacilityID`) REFERENCES `facilities` (`FacilityID`);

--
-- Constraints for table `feedback`
--
ALTER TABLE `feedback`
  ADD CONSTRAINT `feedback_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`);

--
-- Constraints for table `forum_comments`
--
ALTER TABLE `forum_comments`
  ADD CONSTRAINT `fk_forum_comments_posts` FOREIGN KEY (`PostID`) REFERENCES `forum_posts` (`PostID`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_forum_comments_users` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- Constraints for table `forum_comment_votes`
--
ALTER TABLE `forum_comment_votes`
  ADD CONSTRAINT `fk_forum_comment_votes_comments` FOREIGN KEY (`CommentID`) REFERENCES `forum_comments` (`CommentID`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_forum_comment_votes_users` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- Constraints for table `forum_posts`
--
ALTER TABLE `forum_posts`
  ADD CONSTRAINT `forum_posts_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- Constraints for table `forum_post_votes`
--
ALTER TABLE `forum_post_votes`
  ADD CONSTRAINT `fk_forum_post_votes_posts` FOREIGN KEY (`PostID`) REFERENCES `forum_posts` (`PostID`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_forum_post_votes_users` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- Constraints for table `payments`
--
ALTER TABLE `payments`
  ADD CONSTRAINT `payments_ibfk_1` FOREIGN KEY (`BillID`) REFERENCES `bills` (`BillID`) ON DELETE CASCADE,
  ADD CONSTRAINT `payments_ibfk_2` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE,
  ADD CONSTRAINT `payments_ibfk_3` FOREIGN KEY (`VerifiedBy`) REFERENCES `users` (`UserID`) ON DELETE SET NULL;

--
-- Constraints for table `payment_history`
--
ALTER TABLE `payment_history`
  ADD CONSTRAINT `payment_history_ibfk_1` FOREIGN KEY (`PaymentID`) REFERENCES `payments` (`PaymentID`) ON DELETE CASCADE,
  ADD CONSTRAINT `payment_history_ibfk_2` FOREIGN KEY (`PerformedBy`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- Constraints for table `polls`
--
ALTER TABLE `polls`
  ADD CONSTRAINT `polls_ibfk_1` FOREIGN KEY (`CreatedBy`) REFERENCES `users` (`UserID`);

--
-- Constraints for table `poll_options`
--
ALTER TABLE `poll_options`
  ADD CONSTRAINT `poll_options_ibfk_1` FOREIGN KEY (`PollID`) REFERENCES `polls` (`PollID`) ON DELETE CASCADE;

--
-- Constraints for table `poll_responses`
--
ALTER TABLE `poll_responses`
  ADD CONSTRAINT `poll_responses_ibfk_1` FOREIGN KEY (`PollID`) REFERENCES `polls` (`PollID`) ON DELETE CASCADE,
  ADD CONSTRAINT `poll_responses_ibfk_2` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`),
  ADD CONSTRAINT `poll_responses_ibfk_3` FOREIGN KEY (`OptionID`) REFERENCES `poll_options` (`OptionID`);

--
-- Constraints for table `service_requests`
--
ALTER TABLE `service_requests`
  ADD CONSTRAINT `service_requests_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE,
  ADD CONSTRAINT `service_requests_ibfk_2` FOREIGN KEY (`ServiceTypeID`) REFERENCES `service_types` (`ServiceTypeID`) ON DELETE CASCADE;

--
-- Constraints for table `vehicleregistrations`
--
ALTER TABLE `vehicleregistrations`
  ADD CONSTRAINT `vehicleregistrations_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;

--
-- Constraints for table `visitorpasses`
--
ALTER TABLE `visitorpasses`
  ADD CONSTRAINT `visitorpasses_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE,
  ADD CONSTRAINT `visitorpasses_ibfk_2` FOREIGN KEY (`ApprovedBy`) REFERENCES `users` (`UserID`) ON DELETE SET NULL;

--
-- Constraints for table `visitor_passes`
--
ALTER TABLE `visitor_passes`
  ADD CONSTRAINT `visitor_passes_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
