-- Disable foreign key checks temporarily
SET FOREIGN_KEY_CHECKS = 0;

-- Drop data from tables in reverse order of dependencies
DELETE FROM visitor_passes;
DELETE FROM visitorpasses;
DELETE FROM vehicleregistrations;
DELETE FROM service_requests;
DELETE FROM poll_responses;
DELETE FROM poll_options;
DELETE FROM polls;
DELETE FROM forum_post_votes;
DELETE FROM forum_comment_votes;
DELETE FROM forum_comments;
DELETE FROM forum_posts;
DELETE FROM feedback;
DELETE FROM facility_reservations;
DELETE FROM documents;
DELETE FROM contact_directory;
DELETE FROM bills;
DELETE FROM announcements;
DELETE FROM addresses;
DELETE FROM facilities;
DELETE FROM service_types;a
DELETE FROM bill_types;
DELETE FROM users;

-- Reset auto-increment counters
ALTER TABLE visitor_passes AUTO_INCREMENT = 1;
ALTER TABLE visitorpasses AUTO_INCREMENT = 1;
ALTER TABLE vehicleregistrations AUTO_INCREMENT = 1;
ALTER TABLE service_requests AUTO_INCREMENT = 1;
ALTER TABLE poll_responses AUTO_INCREMENT = 1;
ALTER TABLE poll_options AUTO_INCREMENT = 1;
ALTER TABLE polls AUTO_INCREMENT = 1;
ALTER TABLE forum_post_votes AUTO_INCREMENT = 1;
ALTER TABLE forum_comment_votes AUTO_INCREMENT = 1;
ALTER TABLE forum_comments AUTO_INCREMENT = 1;
ALTER TABLE forum_posts AUTO_INCREMENT = 1;
ALTER TABLE feedback AUTO_INCREMENT = 1;
ALTER TABLE facility_reservations AUTO_INCREMENT = 1;
ALTER TABLE documents AUTO_INCREMENT = 1;
ALTER TABLE contact_directory AUTO_INCREMENT = 1;
ALTER TABLE bills AUTO_INCREMENT = 1;
ALTER TABLE announcements AUTO_INCREMENT = 1;
ALTER TABLE addresses AUTO_INCREMENT = 1;
ALTER TABLE facilities AUTO_INCREMENT = 1;
ALTER TABLE service_types AUTO_INCREMENT = 1;
ALTER TABLE bill_types AUTO_INCREMENT = 1;
ALTER TABLE users AUTO_INCREMENT = 1;

-- Re-enable foreign key checks
SET FOREIGN_KEY_CHECKS = 1; 