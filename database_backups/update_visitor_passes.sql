-- Drop existing table if it exists
DROP TABLE IF EXISTS `visitor_passes`;

-- Create new table with updated schema
CREATE TABLE `visitor_passes` (
  `VisitorPassId` int(11) NOT NULL AUTO_INCREMENT,
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
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `ApprovedAt` datetime DEFAULT NULL,
  `ApprovedBy` int(11) DEFAULT NULL,
  `RejectionReason` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`VisitorPassId`),
  KEY `UserID` (`UserID`),
  CONSTRAINT `visitor_passes_ibfk_1` FOREIGN KEY (`UserID`) REFERENCES `users` (`UserID`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci; 