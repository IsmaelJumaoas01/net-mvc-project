-- Insert data into users table
INSERT INTO users (Username, PasswordHash, Email, PhoneNumber, Role, FirstName, MiddleName, LastName, Address) VALUES
('john.doe', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'john.doe@email.com', '1234567890', 'Homeowner', 'John', 'A', 'Doe', '123 Main St'),
('jane.smith', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'jane.smith@email.com', '2345678901', 'Homeowner', 'Jane', 'B', 'Smith', '456 Oak Ave'),
('admin.user', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'admin@email.com', '3456789012', 'Administrator', 'Admin', 'C', 'User', '789 Admin Blvd'),
('staff.user', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'staff@email.com', '4567890123', 'Staff', 'Staff', 'D', 'User', '321 Staff St'),
('mike.johnson', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'mike.j@email.com', '5678901234', 'Homeowner', 'Mike', 'E', 'Johnson', '654 Pine Rd'),
('sarah.wilson', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'sarah.w@email.com', '6789012345', 'Homeowner', 'Sarah', 'F', 'Wilson', '987 Elm St'),
('david.brown', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'david.b@email.com', '7890123456', 'Homeowner', 'David', 'G', 'Brown', '741 Maple Ave'),
('lisa.miller', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'lisa.m@email.com', '8901234567', 'Homeowner', 'Lisa', 'H', 'Miller', '852 Cedar Ln'),
('robert.taylor', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'robert.t@email.com', '9012345678', 'Homeowner', 'Robert', 'I', 'Taylor', '963 Birch Rd'),
('emily.anderson', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'emily.a@email.com', '0123456789', 'Homeowner', 'Emily', 'J', 'Anderson', '159 Willow St'),
('james.wilson', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'james.w@email.com', '1122334455', 'Homeowner', 'James', 'K', 'Wilson', '321 Pine St'),
('mary.johnson', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'mary.j@email.com', '2233445566', 'Homeowner', 'Mary', 'L', 'Johnson', '432 Oak Ave'),
('peter.smith', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'peter.s@email.com', '3344556677', 'Homeowner', 'Peter', 'M', 'Smith', '543 Maple Rd'),
('susan.brown', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'susan.b@email.com', '4455667788', 'Homeowner', 'Susan', 'N', 'Brown', '654 Cedar Ln'),
('michael.davis', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'michael.d@email.com', '5566778899', 'Homeowner', 'Michael', 'O', 'Davis', '765 Birch St'),
('staff2.user', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'staff2@email.com', '6677889900', 'Staff', 'Staff2', 'P', 'User', '876 Staff Ave'),
('staff3.user', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'staff3@email.com', '7788990011', 'Staff', 'Staff3', 'Q', 'User', '987 Staff Blvd'),
('admin2.user', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'admin2@email.com', '8899001122', 'Administrator', 'Admin2', 'R', 'User', '098 Admin St'),
('admin3.user', '703b0a3d6ad75b649a28adde7d83c6251da457549263bc7ff45ec709b0a8448b', 'admin3@email.com', '9900112233', 'Administrator', 'Admin3', 'S', 'User', '109 Admin Ave');

-- Insert data into bill_types table
INSERT INTO bill_types (Name, Description, IsRecurring) VALUES
('Monthly Maintenance', 'Monthly maintenance fee for common areas', 1),
('Water Bill', 'Water consumption charges', 1),
('Electricity Bill', 'Electricity consumption charges', 1),
('Garbage Collection', 'Garbage collection service fee', 1),
('Security Service', 'Security service charges', 1),
('Parking Fee', 'Monthly parking fee', 1),
('Special Assessment', 'One-time special assessment fee', 0),
('Late Payment Fee', 'Fee for late payment', 0),
('Amenity Usage', 'Fee for using community amenities', 0),
('Emergency Repair', 'Emergency repair charges', 0);

-- Insert data into service_types table
INSERT INTO service_types (ServiceTypeName) VALUES
('Plumbing'),
('Electrical'),
('HVAC'),
('Carpentry'),
('Painting'),
('Landscaping'),
('Cleaning'),
('Security'),
('Pest Control'),
('General Maintenance');

-- Insert data into facilities table
INSERT INTO facilities (FacilityName) VALUES
('Swimming Pool'),
('Tennis Court'),
('Basketball Court'),
('Fitness Center'),
('Community Hall'),
('Party Room'),
('BBQ Area'),
('Playground'),
('Parking Garage'),
('Garden Area');

-- Insert data into addresses table
INSERT INTO addresses (UserId, BlockNumber, LotNumber, StreetName, Phase, AdditionalDetails) VALUES
(1, 'A', '101', 'Main Street', 'Phase 1', 'Near Pool'),
(2, 'B', '202', 'Oak Avenue', 'Phase 1', 'Corner Lot'),
(3, 'C', '303', 'Admin Boulevard', 'Phase 2', 'End Unit'),
(4, 'D', '404', 'Staff Street', 'Phase 2', 'Middle Unit'),
(5, 'E', '505', 'Pine Road', 'Phase 3', 'Near Tennis Court'),
(6, 'F', '606', 'Elm Street', 'Phase 3', 'Garden View'),
(7, 'G', '707', 'Maple Avenue', 'Phase 4', 'Parking View'),
(8, 'H', '808', 'Cedar Lane', 'Phase 4', 'Pool View'),
(9, 'I', '909', 'Birch Road', 'Phase 5', 'Corner Unit'),
(10, 'J', '1010', 'Willow Street', 'Phase 5', 'End Unit');

-- Insert data into announcements table
INSERT INTO announcements (Title, Content, UserID) VALUES
('Community Meeting', 'Monthly community meeting scheduled for next week', 3),
('Pool Maintenance', 'Swimming pool will be closed for maintenance', 3),
('Security Update', 'New security measures implemented', 3),
('Holiday Schedule', 'Holiday schedule for community facilities', 3),
('Parking Rules', 'Updated parking rules and regulations', 3),
('Maintenance Schedule', 'Monthly maintenance schedule', 3),
('Event Announcement', 'Community event this weekend', 3),
('Emergency Contact', 'Updated emergency contact numbers', 3),
('Utility Maintenance', 'Scheduled utility maintenance', 3),
('Community Guidelines', 'Updated community guidelines', 3);

-- Insert data into bills table
INSERT INTO bills (UserID, BillTypeID, Amount, DueDate, Status, Description) VALUES
(1, 1, 100.00, '2024-03-01', 'Paid', 'March Maintenance'),
(2, 1, 100.00, '2024-03-01', 'Pending', 'March Maintenance'),
(3, 1, 100.00, '2024-03-01', 'Overdue', 'March Maintenance'),
(4, 1, 100.00, '2024-03-01', 'Paid', 'March Maintenance'),
(5, 1, 100.00, '2024-03-01', 'Pending', 'March Maintenance'),
(6, 1, 100.00, '2024-03-01', 'Paid', 'March Maintenance'),
(7, 1, 100.00, '2024-03-01', 'Pending', 'March Maintenance'),
(8, 1, 100.00, '2024-03-01', 'Paid', 'March Maintenance'),
(9, 1, 100.00, '2024-03-01', 'Pending', 'March Maintenance'),
(10, 1, 100.00, '2024-03-01', 'Paid', 'March Maintenance'),
(11, 1, 100.00, '2024-03-01', 'Paid', 'March Maintenance'),
(12, 1, 100.00, '2024-03-01', 'Pending', 'March Maintenance'),
(13, 1, 100.00, '2024-03-01', 'Overdue', 'March Maintenance'),
(14, 1, 100.00, '2024-03-01', 'Paid', 'March Maintenance'),
(15, 1, 100.00, '2024-03-01', 'Pending', 'March Maintenance'),
(11, 2, 75.50, '2024-03-15', 'Paid', 'February Water Bill'),
(12, 2, 82.25, '2024-03-15', 'Pending', 'February Water Bill'),
(13, 2, 68.75, '2024-03-15', 'Paid', 'February Water Bill'),
(14, 2, 91.00, '2024-03-15', 'Pending', 'February Water Bill'),
(15, 2, 77.50, '2024-03-15', 'Paid', 'February Water Bill'),
(11, 3, 150.00, '2024-03-20', 'Pending', 'February Electricity'),
(12, 3, 175.50, '2024-03-20', 'Paid', 'February Electricity'),
(13, 3, 125.75, '2024-03-20', 'Pending', 'February Electricity'),
(14, 3, 200.00, '2024-03-20', 'Paid', 'February Electricity'),
(15, 3, 165.25, '2024-03-20', 'Pending', 'February Electricity');

-- Insert data into contact_directory table
INSERT INTO contact_directory (Name, Position, Department, PhoneNumber, Email, OfficeLocation, OfficeHours) VALUES
('John Manager', 'Manager', 'Management', '111-222-3333', 'manager@email.com', 'Main Office', '9 AM - 5 PM'),
('Sarah Security', 'Security Head', 'Security', '222-333-4444', 'security@email.com', 'Security Office', '24/7'),
('Mike Maintenance', 'Maintenance Head', 'Maintenance', '333-444-5555', 'maintenance@email.com', 'Maintenance Office', '8 AM - 4 PM'),
('Lisa Admin', 'Admin Assistant', 'Administration', '444-555-6666', 'admin@email.com', 'Admin Office', '9 AM - 5 PM'),
('David Finance', 'Finance Manager', 'Finance', '555-666-7777', 'finance@email.com', 'Finance Office', '9 AM - 5 PM'),
('Emily Events', 'Events Coordinator', 'Events', '666-777-8888', 'events@email.com', 'Events Office', '10 AM - 6 PM'),
('Robert Facilities', 'Facilities Manager', 'Facilities', '777-888-9999', 'facilities@email.com', 'Facilities Office', '8 AM - 4 PM'),
('Jane Community', 'Community Manager', 'Community', '888-999-0000', 'community@email.com', 'Community Office', '9 AM - 5 PM'),
('Mike Technical', 'Technical Support', 'IT', '999-000-1111', 'it@email.com', 'IT Office', '9 AM - 5 PM'),
('Sarah Customer', 'Customer Service', 'Customer Service', '000-111-2222', 'customer@email.com', 'Customer Service Office', '9 AM - 5 PM');

-- Insert data into documents table
INSERT INTO documents (Title, FilePath, UploadedBy) VALUES
('Community Rules', '/documents/rules.pdf', 3),
('Maintenance Schedule', '/documents/maintenance.pdf', 3),
('Emergency Procedures', '/documents/emergency.pdf', 3),
('Parking Rules', '/documents/parking.pdf', 3),
('Facility Guidelines', '/documents/facilities.pdf', 3),
('Security Procedures', '/documents/security.pdf', 3),
('Event Calendar', '/documents/events.pdf', 3),
('Maintenance Request Form', '/documents/request.pdf', 3),
('Visitor Pass Form', '/documents/visitor.pdf', 3),
('Complaint Form', '/documents/complaint.pdf', 3);

-- Insert data into facility_reservations table
INSERT INTO facility_reservations (UserID, ReservationDate, StartTime, EndTime, Status, FacilityID) VALUES
(1, '2024-03-01', '10:00:00', '12:00:00', 'Confirmed', 1),
(2, '2024-03-02', '14:00:00', '16:00:00', 'Pending', 2),
(3, '2024-03-03', '16:00:00', '18:00:00', 'Confirmed', 3),
(4, '2024-03-04', '18:00:00', '20:00:00', 'Pending', 4),
(5, '2024-03-05', '20:00:00', '22:00:00', 'Confirmed', 5),
(6, '2024-03-06', '10:00:00', '12:00:00', 'Pending', 6),
(7, '2024-03-07', '14:00:00', '16:00:00', 'Confirmed', 7),
(8, '2024-03-08', '16:00:00', '18:00:00', 'Pending', 8),
(9, '2024-03-09', '18:00:00', '20:00:00', 'Confirmed', 9),
(10, '2024-03-10', '20:00:00', '22:00:00', 'Pending', 10);

-- Insert data into feedback table
INSERT INTO feedback (UserID, Title, Content, Status) VALUES
(1, 'Pool Maintenance', 'Pool needs cleaning', 'Pending'),
(2, 'Security Issue', 'Security camera not working', 'Pending'),
(3, 'Parking Problem', 'Visitor parking full', 'Pending'),
(4, 'Noise Complaint', 'Loud music at night', 'Pending'),
(5, 'Garbage Collection', 'Missed garbage collection', 'Pending'),
(6, 'Maintenance Request', 'Leaking faucet', 'Pending'),
(7, 'Security Concern', 'Suspicious activity', 'Pending'),
(8, 'Facility Issue', 'Broken equipment', 'Pending'),
(9, 'Community Event', 'Great event organization', 'Pending'),
(10, 'Staff Service', 'Excellent service', 'Pending'),
(11, 'Pool Maintenance', 'Pool needs regular cleaning', 'Pending'),
(12, 'Security Issue', 'Broken security gate', 'Pending'),
(13, 'Parking Problem', 'Visitor parking full', 'Pending'),
(14, 'Noise Complaint', 'Loud music at night', 'Pending'),
(15, 'Garbage Collection', 'Missed collection', 'Pending'),
(11, 'Facility Issue', 'Broken gym equipment', 'Pending'),
(12, 'Community Event', 'Great event organization', 'Pending'),
(13, 'Staff Service', 'Excellent maintenance service', 'Pending'),
(14, 'Security Concern', 'Suspicious activity', 'Pending'),
(15, 'Maintenance Request', 'Leaking roof', 'Pending');

-- Insert data into forum_posts table
INSERT INTO forum_posts (UserID, Title, Content) VALUES
(1, 'Community Meeting', 'Discussion about upcoming meeting'),
(2, 'Security Update', 'New security measures implemented'),
(3, 'Maintenance Schedule', 'Monthly maintenance schedule'),
(4, 'Event Planning', 'Planning for community event'),
(5, 'Parking Rules', 'Updated parking rules'),
(6, 'Facility Usage', 'Facility usage guidelines'),
(7, 'Community Guidelines', 'Updated community guidelines'),
(8, 'Emergency Procedures', 'Emergency procedures update'),
(9, 'Maintenance Request', 'Maintenance request process'),
(10, 'Visitor Policy', 'Updated visitor policy'),
(11, 'Community Garden Project', 'Proposal for community garden development'),
(12, 'Parking Space Allocation', 'Discussion about visitor parking rules'),
(13, 'Security Camera Installation', 'Feedback on new security system'),
(14, 'Community Event Planning', 'Ideas for summer community event'),
(15, 'Maintenance Schedule', 'Suggestions for maintenance timing'),
(11, 'Noise Complaint Policy', 'Discussion about noise regulations'),
(12, 'Facility Booking System', 'Feedback on new booking system'),
(13, 'Emergency Procedures', 'Review of emergency protocols'),
(14, 'Community Guidelines', 'Proposed updates to guidelines'),
(15, 'Visitor Policy', 'Discussion about visitor management');

-- Insert data into forum_comments table
INSERT INTO forum_comments (PostID, UserID, Content) VALUES
(1, 2, 'Great initiative'),
(1, 3, 'Looking forward to it'),
(2, 4, 'Good security measures'),
(2, 5, 'Well implemented'),
(3, 6, 'Clear schedule'),
(3, 7, 'Well organized'),
(4, 8, 'Exciting event'),
(4, 9, 'Count me in'),
(5, 10, 'Clear rules'),
(5, 1, 'Well explained'),
(11, 12, 'Great initiative for the garden project'),
(11, 13, 'I can help with the planning'),
(12, 14, 'Good points about parking'),
(12, 15, 'We need better enforcement'),
(13, 11, 'Security cameras are working well'),
(13, 12, 'Some areas need more coverage'),
(14, 13, 'Exciting event ideas'),
(14, 14, 'Count me in for organizing'),
(15, 15, 'Maintenance timing works for me'),
(15, 11, 'Need to adjust some timings');

-- Insert data into forum_comment_votes table
INSERT INTO forum_comment_votes (UserID, CommentID, VoteValue) VALUES
(1, 1, 1),
(2, 2, 1),
(3, 3, 1),
(4, 4, 1),
(5, 5, 1),
(6, 6, 1),
(7, 7, 1),
(8, 8, 1),
(9, 9, 1),
(10, 10, 1);

-- Insert data into forum_post_votes table
INSERT INTO forum_post_votes (UserID, PostID, VoteValue) VALUES
(1, 1, 1),
(2, 2, 1),
(3, 3, 1),
(4, 4, 1),
(5, 5, 1),
(6, 6, 1),
(7, 7, 1),
(8, 8, 1),
(9, 9, 1),
(10, 10, 1);

-- Insert data into polls table
INSERT INTO polls (Title, Description, StartDate, EndDate, CreatedBy) VALUES
('Community Event', 'Vote for next community event', '2024-03-01', '2024-03-15', 3),
('Facility Hours', 'Vote for facility hours', '2024-03-01', '2024-03-15', 3),
('Maintenance Schedule', 'Vote for maintenance schedule', '2024-03-01', '2024-03-15', 3),
('Security Measures', 'Vote for security measures', '2024-03-01', '2024-03-15', 3),
('Parking Rules', 'Vote for parking rules', '2024-03-01', '2024-03-15', 3),
('Community Guidelines', 'Vote for community guidelines', '2024-03-01', '2024-03-15', 3),
('Event Planning', 'Vote for event planning', '2024-03-01', '2024-03-15', 3),
('Facility Usage', 'Vote for facility usage', '2024-03-01', '2024-03-15', 3),
('Emergency Procedures', 'Vote for emergency procedures', '2024-03-01', '2024-03-15', 3),
('Visitor Policy', 'Vote for visitor policy', '2024-03-01', '2024-03-15', 3);

-- Insert data into poll_options table
INSERT INTO poll_options (PollID, OptionText) VALUES
(1, 'BBQ Party'),
(1, 'Movie Night'),
(1, 'Sports Day'),
(2, '8 AM - 8 PM'),
(2, '9 AM - 9 PM'),
(2, '10 AM - 10 PM'),
(3, 'Weekly'),
(3, 'Bi-weekly'),
(3, 'Monthly'),
(4, 'More Cameras'),
(4, 'Security Guards'),
(4, 'Access Cards'),
(5, 'Strict Rules'),
(5, 'Flexible Rules'),
(5, 'No Rules'),
(6, 'Strict Guidelines'),
(6, 'Flexible Guidelines'),
(6, 'No Guidelines'),
(7, 'Monthly Events'),
(7, 'Quarterly Events'),
(7, 'Annual Events'),
(8, 'Strict Usage'),
(8, 'Flexible Usage'),
(8, 'No Restrictions'),
(9, 'Strict Procedures'),
(9, 'Flexible Procedures'),
(9, 'No Procedures'),
(10, 'Strict Policy'),
(10, 'Flexible Policy'),
(10, 'No Policy');

-- Insert data into poll_responses table
INSERT INTO poll_responses (PollID, UserID, OptionID) VALUES
(1, 1, 1),
(1, 2, 2),
(1, 3, 3),
(2, 4, 4),
(2, 5, 5),
(2, 6, 6),
(3, 7, 7),
(3, 8, 8),
(3, 9, 9),
(4, 10, 10);

-- Insert data into service_requests table
INSERT INTO service_requests (UserID, Description, Status, ServiceTypeID) VALUES
(1, 'Leaking faucet in kitchen', 'Pending', 1),
(2, 'Electrical outlet not working', 'Pending', 2),
(3, 'AC not cooling properly', 'Pending', 3),
(4, 'Door hinge broken', 'Pending', 4),
(5, 'Wall needs painting', 'Pending', 5),
(6, 'Garden needs maintenance', 'Pending', 6),
(7, 'House needs cleaning', 'Pending', 7),
(8, 'Security camera not working', 'Pending', 8),
(9, 'Pest control needed', 'Pending', 9),
(10, 'General maintenance required', 'Pending', 10),
(11, 'Kitchen sink leaking', 'Pending', 1),
(12, 'Power outage in living room', 'Pending', 2),
(13, 'AC not working properly', 'Pending', 3),
(14, 'Door lock needs repair', 'Pending', 4),
(15, 'Wall paint peeling', 'Pending', 5),
(11, 'Garden needs maintenance', 'Pending', 6),
(12, 'House needs deep cleaning', 'Pending', 7),
(13, 'Security camera malfunction', 'Pending', 8),
(14, 'Ant infestation in kitchen', 'Pending', 9),
(15, 'General maintenance check', 'Pending', 10),
(11, 'Bathroom faucet dripping', 'Pending', 1),
(12, 'Light fixture not working', 'Pending', 2),
(13, 'Heater making strange noise', 'Pending', 3),
(14, 'Window frame repair needed', 'Pending', 4),
(15, 'Ceiling paint touch-up', 'Pending', 5);

-- Insert data into vehicleregistrations table
INSERT INTO vehicleregistrations (UserID, PlateNumber, VehicleModel, VehicleColor, VehicleType, VehicleBrand, ExpiryDate, Status) VALUES
(1, 'ABC123', 'Camry', 'Black', 'Sedan', 'Toyota', '2024-12-31', 'Active'),
(2, 'DEF456', 'Civic', 'White', 'Sedan', 'Honda', '2024-12-31', 'Active'),
(3, 'GHI789', 'Accord', 'Silver', 'Sedan', 'Honda', '2024-12-31', 'Active'),
(4, 'JKL012', 'Corolla', 'Blue', 'Sedan', 'Toyota', '2024-12-31', 'Active'),
(5, 'MNO345', 'Altima', 'Red', 'Sedan', 'Nissan', '2024-12-31', 'Active'),
(6, 'PQR678', 'Sonata', 'Gray', 'Sedan', 'Hyundai', '2024-12-31', 'Active'),
(7, 'STU901', 'Elantra', 'Green', 'Sedan', 'Hyundai', '2024-12-31', 'Active'),
(8, 'VWX234', 'Forte', 'Yellow', 'Sedan', 'Kia', '2024-12-31', 'Active'),
(9, 'YZA567', 'Mazda3', 'Purple', 'Sedan', 'Mazda', '2024-12-31', 'Active'),
(10, 'BCD890', 'Sentra', 'Orange', 'Sedan', 'Nissan', '2024-12-31', 'Active'),
(11, 'XYZ123', 'Civic', 'Blue', 'Sedan', 'Honda', '2024-12-31', 'Active'),
(12, 'ABC456', 'Corolla', 'Silver', 'Sedan', 'Toyota', '2024-12-31', 'Active'),
(13, 'DEF789', 'Accord', 'Black', 'Sedan', 'Honda', '2024-12-31', 'Active'),
(14, 'GHI012', 'Camry', 'White', 'Sedan', 'Toyota', '2024-12-31', 'Active'),
(15, 'JKL345', 'Altima', 'Red', 'Sedan', 'Nissan', '2024-12-31', 'Active');

-- Insert data into visitorpasses table
INSERT INTO visitorpasses (UserID, VisitorName, VisitorContact, Purpose, VisitDate, VisitTime, ExpiryDate, Status) VALUES
(1, 'John Visitor', '111-222-3333', 'Family Visit', '2024-03-01', '10:00:00', '2024-03-01', 'Approved'),
(2, 'Jane Visitor', '222-333-4444', 'Friend Visit', '2024-03-02', '14:00:00', '2024-03-02', 'Pending'),
(3, 'Mike Visitor', '333-444-5555', 'Business Meeting', '2024-03-03', '16:00:00', '2024-03-03', 'Approved'),
(4, 'Sarah Visitor', '444-555-6666', 'Family Visit', '2024-03-04', '18:00:00', '2024-03-04', 'Pending'),
(5, 'David Visitor', '555-666-7777', 'Friend Visit', '2024-03-05', '20:00:00', '2024-03-05', 'Approved'),
(6, 'Lisa Visitor', '666-777-8888', 'Business Meeting', '2024-03-06', '10:00:00', '2024-03-06', 'Pending'),
(7, 'Robert Visitor', '777-888-9999', 'Family Visit', '2024-03-07', '14:00:00', '2024-03-07', 'Approved'),
(8, 'Emily Visitor', '888-999-0000', 'Friend Visit', '2024-03-08', '16:00:00', '2024-03-08', 'Pending'),
(9, 'Mike Visitor', '999-000-1111', 'Business Meeting', '2024-03-09', '18:00:00', '2024-03-09', 'Approved'),
(10, 'Sarah Visitor', '000-111-2222', 'Family Visit', '2024-03-10', '20:00:00', '2024-03-10', 'Pending'),
(11, 'Tom Visitor', '111-222-3333', 'Family Visit', '2024-03-11', '10:00:00', '2024-03-11', 'Approved'),
(12, 'Jerry Visitor', '222-333-4444', 'Friend Visit', '2024-03-12', '14:00:00', '2024-03-12', 'Pending'),
(13, 'Mike Visitor', '333-444-5555', 'Business Meeting', '2024-03-13', '16:00:00', '2024-03-13', 'Approved'),
(14, 'Sarah Visitor', '444-555-6666', 'Family Visit', '2024-03-14', '18:00:00', '2024-03-14', 'Pending'),
(15, 'David Visitor', '555-666-7777', 'Friend Visit', '2024-03-15', '20:00:00', '2024-03-15', 'Approved'),
(11, 'Lisa Visitor', '666-777-8888', 'Business Meeting', '2024-03-16', '10:00:00', '2024-03-16', 'Pending'),
(12, 'Robert Visitor', '777-888-9999', 'Family Visit', '2024-03-17', '14:00:00', '2024-03-17', 'Approved'),
(13, 'Emily Visitor', '888-999-0000', 'Friend Visit', '2024-03-18', '16:00:00', '2024-03-18', 'Pending'),
(14, 'John Visitor', '999-000-1111', 'Business Meeting', '2024-03-19', '18:00:00', '2024-03-19', 'Approved'),
(15, 'Jane Visitor', '000-111-2222', 'Family Visit', '2024-03-20', '20:00:00', '2024-03-20', 'Pending'); 