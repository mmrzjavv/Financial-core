-- Run BEFORE applying EF migration: RedesignDataEntry2Form

BEGIN;

DELETE FROM "Cases".case_data_entry_2;

COMMIT;
