CREATE TABLE IF NOT EXISTS public."Users"
(
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    password VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS public."MailTasks"
(
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    text TEXT NOT NULL,
    email_data VARCHAR(100) NULL,
    phone_data VARCHAR(11) NULL,
    personal_number INT NULL,
    created_time TIMESTAMP DEFAULT NOW() NOT NULL,
    updated_time TIMESTAMP DEFAULT NOW() NOT NULL,
    status VARCHAR(10) NOT NULL,
    CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES public."Users"(id)
);

CREATE TABLE IF NOT EXISTS public."MailTasksАrchive"
(
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL,
    text TEXT NOT NULL,
    email_data VARCHAR(100) NULL,
    phone_data VARCHAR(11) NULL,
    personal_number INT NULL,
    archiving_time TIMESTAMP DEFAULT NOW() NOT NULL
);
