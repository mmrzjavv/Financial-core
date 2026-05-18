-- Run BEFORE applying EF migration: RedesignDataEntry1Form
-- Clears legacy data-entry-1 rows and optional old applicant documents from the previous form.

BEGIN;

DELETE FROM "Cases".case_data_entry_1;

-- Optional: remove documents that belonged to the old DE1 checklist (not pitch deck / business plan / other)
DELETE FROM "Cases".case_documents
WHERE "DocumentType" IN (2, 3, 4, 5, 6);

COMMIT;
