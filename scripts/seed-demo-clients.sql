-- Demo clients for pagination testing. Safe to re-run.
DO $$
DECLARE
    existing_count integer;
    to_create integer;
    i integer;
    start_index integer;
BEGIN
    SELECT COUNT(*) INTO existing_count
    FROM "Clients"
    WHERE "Notes" = 'pagination-demo';

    IF existing_count >= 100 THEN
        RAISE NOTICE 'Already have % demo clients', existing_count;
        RETURN;
    END IF;

    to_create := 100 - existing_count;
    start_index := existing_count + 1;

    FOR i IN 0..(to_create - 1) LOOP
        INSERT INTO "Clients" (
            "Id",
            "FullName",
            "PhoneNumber",
            "Notes",
            "CreatedAtUtc",
            "IsArchived",
            "ArchivedAtUtc")
        VALUES (
            gen_random_uuid(),
            'Демо клиент ' || LPAD((start_index + i)::text, 3, '0'),
            '+7900' || LPAD((start_index + i)::text, 7, '0'),
            'pagination-demo',
            NOW() AT TIME ZONE 'utc' - ((start_index + i) || ' minutes')::interval,
            FALSE,
            NULL);
    END LOOP;

    RAISE NOTICE 'Created % demo clients, total %', to_create, existing_count + to_create;
END $$;
